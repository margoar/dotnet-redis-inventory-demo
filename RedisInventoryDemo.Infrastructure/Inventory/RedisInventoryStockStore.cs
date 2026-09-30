using RedisInventoryDemo.Application.Contracts.Inventory;
using StackExchange.Redis;

namespace RedisInventoryDemo.Infrastructure.Inventory;

public sealed class RedisInventoryStockStore : IInventoryStockStore
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisInventoryStockStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

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