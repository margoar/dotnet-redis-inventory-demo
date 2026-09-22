using RedisInventoryDemo.Application.Abstractions.Caching;
using StackExchange.Redis;
using RedisInventoryDemo.Application.Contracts.Inventory;

namespace RedisInventoryDemo.Infrastructure.Caching;

public sealed class RedisInventoryCache : IInventoryCache
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private const string ReserveStockScript = """
    local stock = redis.call('GET', KEYS[1])

    if not stock then
        return -1
    end

    if tonumber(stock) < tonumber(ARGV[1]) then
        return -2
    end

    local newStock = redis.call(
        'DECRBY',
        KEYS[1],
        ARGV[1]
    )

    return newStock
    """;
    public RedisInventoryCache(
        IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<int?> GetStockAsync(int inventoryId)
    {
        var database = _connectionMultiplexer.GetDatabase();

        var key = $"inventory:{inventoryId}";

        var value = await database.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return null;

        return int.Parse(value!);
    }

    public async Task SetStockAsync(int inventoryId, int stock, TimeSpan? expiration = null)
    {
        var database = _connectionMultiplexer.GetDatabase();

        var key = $"inventory:{inventoryId}";
        if (expiration.HasValue)
        {
            await database.StringSetAsync(
                key,
                stock,
                new Expiration(expiration.Value));

            return;
        }

        await database.StringSetAsync(
            key,
            stock);
    }
    public async Task<ReserveStockResult> TryReserveStockAsync(int inventoryId, int quantity)
    {
        var database = _connectionMultiplexer.GetDatabase();

        var key = $"inventory:{inventoryId}";

        var result = await database.ScriptEvaluateAsync(
            ReserveStockScript,
            [key],
            [quantity]);

        var value = (long)result;

        return value switch
        {
            -1 => new ReserveStockResult(
                ReserveStockStatus.InventoryNotFound,
                null),

            -2 => new ReserveStockResult(
                ReserveStockStatus.InsufficientStock,
                null),

            _ => new ReserveStockResult(
                ReserveStockStatus.Reserved,
                (int)value)
        };
    }
}