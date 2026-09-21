using RedisInventoryDemo.Application.Abstractions.Caching;
using StackExchange.Redis;

namespace RedisInventoryDemo.Infrastructure.Caching;

public sealed class RedisInventoryCache : IInventoryCache
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

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
}