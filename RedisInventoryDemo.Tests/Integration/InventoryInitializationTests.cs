using RedisInventoryDemo.Application.Services;
using RedisInventoryDemo.Infrastructure.Caching;
using RedisInventoryDemo.Infrastructure.Persistence;
using StackExchange.Redis;

namespace RedisInventoryDemo.Tests.Integration;

public class InventoryInitializationTests
{
    [Fact]
    public async Task ReserveAsync_ShouldInitializeStockWhenRedisKeyDoesNotExist()
    {
        // Arrange
        var connection = await ConnectionMultiplexer.ConnectAsync("localhost:6380");

        var database = connection.GetDatabase();

        const int inventoryId = 1;

        await database.KeyDeleteAsync($"inventory:{inventoryId}");

        var cache = new RedisInventoryCache(connection);
        var repository = new InMemoryInventoryRepository();

        var service = new InventoryService(
            repository,
            cache);

        // Act
        var result = await service.ReserveAsync(inventoryId, new Application.Contracts.Inventory.ReserveInventoryRequest(3));

        // Assert
        Assert.Equal(Application.Contracts.Inventory.ReserveStockStatus.Reserved, result.Status);
        Assert.Equal(7, result.RemainingStock);

        var stockInRedis = await database.StringGetAsync($"inventory:{inventoryId}");

        Assert.Equal("7",  stockInRedis);


        await database.KeyDeleteAsync($"inventory:{inventoryId}");
        await connection.CloseAsync();
    }
}