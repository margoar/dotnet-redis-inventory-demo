# dotnet-redis-inventory-demo

Aplicación educativa construida con .NET 8 para aprender el uso de **Redis** aplicado a un escenario de inventario.

## Descripción

El proyecto expone una API REST sencilla que permite consultar el stock de un producto y reservar unidades de ese stock. Redis se utiliza como caché de lectura (patrón Cache-Aside) y como motor de la operación crítica de reserva, que se resuelve de forma atómica mediante un script Lua ejecutado dentro de Redis.

## Objetivo actual

Entender, sobre un caso concreto y pequeño, dos usos fundamentales de Redis:

1. Usar Redis como caché de consulta con expiración (Cache-Aside).
2. Ejecutar una validación y un decremento de stock de forma atómica con un script Lua.

Todo está organizado en una arquitectura por capas con inyección de dependencias, para que cada responsabilidad quede separada y sea fácil de seguir.

## Tecnologías utilizadas

- .NET 8
- ASP.NET Core Web API
- Redis
- StackExchange.Redis
- Docker (para ejecutar Redis localmente)
- Arquitectura por capas
- Dependency Injection

## Arquitectura

La solución está dividida en cuatro proyectos:

| Proyecto | Responsabilidad |
| --- | --- |
| `RedisInventoryDemo.Domain` | Entidades del dominio. |
| `RedisInventoryDemo.Application` | Contratos, abstracciones y lógica de aplicación. |
| `RedisInventoryDemo.Infrastructure` | Implementación de Redis y persistencia. |
| `RedisInventoryDemo.Api` | Expone los endpoints HTTP. |

Dependencias entre proyectos:

```
Api            -> Application -> Domain
Infrastructure -> Application -> Domain
```

La capa de aplicación define las abstracciones (`IInventoryCache`, `IInventoryRepository`) y la capa de infraestructura las implementa, de modo que la lógica de aplicación no depende de Redis ni del origen de datos.

## Funcionalidades implementadas

### 1. Consulta de inventario

`GET /api/inventory/{id}` devuelve el inventario usando Redis como caché, con el patrón Cache-Aside:

1. Consulta Redis.
2. Si el stock existe en Redis, lo devuelve desde ahí.
3. Si no existe, consulta el repositorio.
4. Guarda el stock en Redis con expiración.
5. Devuelve el inventario.

El repositorio utilizado actualmente es un repositorio **en memoria** con algunos productos de ejemplo.

### 2. Reserva de inventario

`POST /api/inventory/{id}/reserve` descuenta unidades del stock almacenado en Redis. La operación crítica (validar y descontar) se ejecuta con un script Lua directamente en Redis.

El script:

- obtiene el stock actual;
- verifica si existe;
- verifica si hay stock suficiente;
- descuenta la cantidad con `DECRBY`;
- devuelve el stock restante cuando la operación es exitosa.

Valores devueltos por el script:

| Valor | Significado |
| --- | --- |
| `-1` | El inventario no existe. |
| `-2` | No hay stock suficiente. |
| `>= 0` | Reserva exitosa; el valor es el stock restante. |

### 3. Resultado de la reserva

El resultado de la reserva se modela con `ReserveStockStatus`, que tiene los estados:

- `InventoryNotFound`
- `InsufficientStock`
- `Reserved`

y con `ReserveStockResult`, que contiene:

- `Status`
- `RemainingStock`

## Endpoints disponibles

### `GET /api/inventory/{id}`

Devuelve el inventario del producto indicado.

- `200 OK` con el inventario.
- `404 Not Found` si el inventario no existe.

### `POST /api/inventory/{id}/reserve`

Reserva unidades del inventario indicado.

Cuerpo de la petición:

```json
{
  "quantity": 3
}
```

Respuestas:

- `200 OK` cuando la reserva fue exitosa.
- `409 Conflict` cuando no hay stock suficiente.
- `404 Not Found` cuando el inventario no existe.

## Cómo ejecutar Redis localmente

Redis se ejecuta con Docker y se expone en el puerto **6380** del host:

```bash
docker run -d --name redis-inventory -p 6380:6379 redis
```

## Cómo configurar la conexión a Redis

La conexión se configura en `appsettings.json` mediante la clave `Redis:ConnectionString`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6380"
  }
}
```

Para ejecutar la API:

```bash
dotnet run --project RedisInventoryDemo.Api
```

En el entorno de desarrollo la API expone Swagger UI en `/swagger`.

## Ejemplos de uso

Consultar inventario:

```bash
curl http://localhost:5059/api/inventory/1
```

```json
{
  "id": 1,
  "name": "Producto 1",
  "stock": 10
}
```

Reservar stock:

```bash
curl -X POST http://localhost:5059/api/inventory/1/reserve \
  -H "Content-Type: application/json" \
  -d "{\"quantity\": 3}"
```

```json
{
  "id": 1,
  "name": "Producto 1",
  "stock": 7
}
```

Si no hay stock suficiente, la respuesta es `409 Conflict`. Si el inventario no existe en Redis, la respuesta es `404 Not Found`.

## Claves en Redis

Las claves de inventario usan el formato `inventory:{id}`, por ejemplo:

```
inventory:1
```

El stock se almacena directamente como un valor numérico, y las entradas pueden tener expiración mediante TTL.

## Cache-Aside en breve

Con Cache-Aside es la aplicación —no la caché— quien decide qué se guarda y cuándo. Ante una lectura, primero se pregunta a Redis; si el dato está, se devuelve sin tocar el origen de datos. Si no está, se lee del repositorio, se escribe en Redis con una expiración y se devuelve. La expiración evita que un dato desactualizado quede indefinidamente en la caché.

## Reserva atómica con Lua en breve

Una reserva implica leer el stock, comprobar que alcanza y descontarlo. Si esos tres pasos se hicieran como comandos separados desde la aplicación, entre la lectura y el descuento podría colarse otra operación sobre la misma clave.

Redis ejecuta un script Lua como una sola operación: la lectura, la validación y el `DECRBY` ocurren juntos, sin que nada más se intercale. Por eso el script no solo descuenta, sino que también valida, y comunica el desenlace mediante su valor de retorno (`-1`, `-2` o el stock restante), que la aplicación traduce a `ReserveStockResult`.

## Estructura de proyectos

```
dotnet-redis-inventory-demo.sln
├── RedisInventoryDemo.Api
│   ├── Controllers/InventoryController.cs
│   ├── Program.cs
│   └── appsettings.json
├── RedisInventoryDemo.Application
│   ├── Abstractions/Caching/IInventoryCache.cs
│   ├── Abstractions/Persistence/IInventoryRepository.cs
│   ├── Contracts/Inventory/ReserveInventoryRequest.cs
│   ├── Contracts/Inventory/ReserveStockResult.cs
│   └── Services/InventoryService.cs
├── RedisInventoryDemo.Domain
│   └── Entities/InventoryItem.cs
└── RedisInventoryDemo.Infrastructure
    ├── Caching/RedisInventoryCache.cs
    ├── Configuracion/RedisOptions.cs
    ├── Persistence/InMemoryInventoryRepository.cs
    └── DependencyInjection.cs
```

---

Este README es una versión temporal y se actualizará a medida que el proyecto avance.
