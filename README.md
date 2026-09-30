# dotnet-redis-inventory-demo

Proyecto educativo en .NET 8 que usa **Redis** como almacén de stock de un inventario.
Expone una API mínima para consultar stock y reservar unidades, y sirve para practicar tres
mecanismos concretos de Redis: Cache-Aside con TTL, reserva atómica mediante Lua y un
distributed lock basado en `SET NX` con token de ownership.

## Arquitectura

```
Api                Infrastructure
 ↓                  ↓
Application        Application
 ↓                  ↓
Domain             Domain
```

| Proyecto | Responsabilidad |
| --- | --- |
| `RedisInventoryDemo.Domain` | Entidad `InventoryItem`. |
| `RedisInventoryDemo.Application` | Abstracciones (`IInventoryCache`, `IInventoryStockStore`, `IDistributedLock`, `IInventoryRepository`), contratos y `InventoryService`. |
| `RedisInventoryDemo.Infrastructure` | Implementaciones Redis (`RedisInventoryCache`, `RedisInventoryStockStore`, `RedisDistributedLock`), repositorio en memoria y registro de DI. |
| `RedisInventoryDemo.Api` | `InventoryController` y composición de la aplicación. |
| `RedisInventoryDemo.Tests` | Pruebas unitarias y de integración (xUnit). |

La persistencia de origen es `InMemoryInventoryRepository`, con dos productos de ejemplo
(`1` con stock 10 y `2` con stock 20). No hay base de datos.

## API

| Endpoint | Respuestas |
| --- | --- |
| `GET /api/inventory/{id}` | `200` con `{ id, name, stock }` · `404` si no existe |
| `POST /api/inventory/{id}/reserve` | `200` con el stock restante · `400` cantidad inválida · `404` inventario inexistente · `409` stock insuficiente |

Cuerpo de la reserva: `{ "quantity": 3 }`

## Redis

| Uso | Implementación |
| --- | --- |
| Almacenamiento del stock | Clave `inventory:{id}`, valor numérico (`RedisInventoryCache`). |
| Cache-Aside | Lectura de la clave; si falta, se carga del repositorio y se escribe en Redis. |
| TTL | Las escrituras del stock usan expiración de 10 minutos. |
| Reserva atómica | Script Lua ejecutado con `ScriptEvaluateAsync` (`RedisInventoryStockStore`). |
| Distributed lock | `SET NX` + expiración y liberación con Lua (`RedisDistributedLock`). |

## Flujo de reserva

```
POST /api/inventory/{id}/reserve
  → cantidad <= 0                      → 400
  → GET inventory:{id}
      ├── existe        → continúa
      └── no existe     → repositorio
                            ├── null   → 404
                            └── item   → SET inventory:{id} EX 600
  → script Lua (GET + validación + DECRBY)
      ├── -1            → 404
      ├── -2            → 409
      └── >= 0          → 200 { stock restante }
```

El script Lua es la única operación que modifica el stock. Lee la clave, valida existencia y
suficiencia, y aplica `DECRBY` dentro de la misma ejecución, de modo que nada se intercala
entre la validación y el decremento. Codifica el desenlace en su valor de retorno
(`-1`, `-2` o el stock restante), que la infraestructura traduce a `ReserveStockResult`
con los estados `InvalidQuantity`, `InventoryNotFound`, `InsufficientStock` y `Reserved`.

La inicialización desde el repositorio ocurre tanto en la consulta como en la reserva: si la
clave no está en Redis (nunca se cargó o venció el TTL), se repuebla antes de reservar.

## Distributed Lock

`RedisDistributedLock` implementa el patrón completo, se registra en DI y está cubierto por
tests, pero **el flujo de reserva no lo utiliza**: la atomicidad la aporta el script Lua.
Hoy es una pieza reutilizable y un ejercicio verificado.

- **Adquisición**: `SET key token NX EX <expiration>`; devuelve el token si la clave no existía, `null` si ya hay dueño.
- **Token de ownership**: un `Guid` por adquisición, que identifica al dueño del lock.
- **Expiración**: obligatoria al adquirir, para que el lock no quede colgado si el dueño no lo libera.
- **Liberación segura**: script Lua que compara el valor con el token y solo entonces hace `DEL`, evitando que un proceso libere el lock de otro.

## Tests

`dotnet test`

**Unitarios** (`Services/InventoryServiceTests`) — `InventoryService` con dobles en memoria, sin Redis:
reserva exitosa, stock insuficiente, inventario inexistente y cantidad inválida.

**Integración** (`Integration/`) — requieren un Redis real en `localhost:6380`, y se agrupan en la
colección `Redis integration`, que desactiva la paralelización entre ellos:

| Escenario | Verifica |
| --- | --- |
| Cache-Aside | La consulta recarga el stock del repositorio y lo deja escrito en Redis cuando la clave no existe. |
| Inicialización | La reserva repuebla la clave desde el repositorio y deja el stock descontado (10 → 7). |
| Concurrencia | 20 reservas simultáneas de 1 unidad sobre stock 10: 10 aceptadas, 10 rechazadas, stock final 0. |
| TTL | La clave desaparece al vencer su expiración. |
| Distributed lock | Un solo dueño simultáneo, readquisición tras liberar, y un token ajeno no libera el lock. |

## Ejecución

```bash
# Redis (puerto 6380 del host, el que espera la configuración)
docker run -d --name redis-inventory -p 6380:6379 redis

# API
dotnet run --project RedisInventoryDemo.Api
```

La conexión se configura en `appsettings.json`:

```json
"Redis": { "ConnectionString": "localhost:6380" }
```

En desarrollo la API sirve Swagger UI en `/swagger` (perfil `http`: `http://localhost:5059`).

## Tecnologías

.NET 8 · ASP.NET Core Web API · StackExchange.Redis · Redis (Docker) · xUnit · Swashbuckle
