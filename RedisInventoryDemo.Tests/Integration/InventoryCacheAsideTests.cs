using RedisInventoryDemo.Application.Services;
using RedisInventoryDemo.Infrastructure.Caching;
using RedisInventoryDemo.Infrastructure.Persistence;
using StackExchange.Redis;
using static RedisInventoryDemo.Tests.Services.InventoryServiceTests;

namespace RedisInventoryDemo.Tests.Integration;
[Collection("Redis integration")]
public class InventoryCacheAsideTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReloadStock_WhenRedisKeyDoesNotExist()
    {
        // Arrange
        var connection = await ConnectionMultiplexer.ConnectAsync("localhost:6380");

        var database = connection.GetDatabase();

        const int inventoryId = 1;
        var key = $"inventory:{inventoryId}";

        await database.KeyDeleteAsync(key);

        var cache = new RedisInventoryCache(connection);
        var repository = new InMemoryInventoryRepository();
        var stockStore = new FakeInventoryStockStore();
        var service = new InventoryService(repository, cache, stockStore);

        // Act
        var result = await service.GetByIdAsync(inventoryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Stock);

        var stockInRedis = await database.StringGetAsync(key);

        Assert.Equal("10", stockInRedis);

        await database.KeyDeleteAsync(key);
        await connection.CloseAsync();
    }
}