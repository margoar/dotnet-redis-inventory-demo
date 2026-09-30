using RedisInventoryDemo.Infrastructure.Caching;
using StackExchange.Redis;

namespace RedisInventoryDemo.Tests.Integration;

public class InventoryConcurrencyTests
{
    [Fact]
    public async Task ConcurrentReservations_ShouldNeverOverReserveStock()
    {
        // Arrange
        var connection = await ConnectionMultiplexer.ConnectAsync(
            "localhost:6380");

        var database = connection.GetDatabase();

        await database.StringSetAsync(
            "inventory:1",
            10);

        var cache = new RedisInventoryCache(connection);

        // Act
        var tasks = Enumerable
            .Range(1, 20)
            .Select(_ =>
                cache.TryReserveStockAsync(1, 1));

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulReservations = results
            .Count(x =>
                x.Status ==
                RedisInventoryDemo.Application.Contracts.Inventory.ReserveStockStatus.Reserved);

        var rejectedReservations = results
            .Count(x =>
                x.Status ==
                RedisInventoryDemo.Application.Contracts.Inventory.ReserveStockStatus.InsufficientStock);

        var finalStock = await database.StringGetAsync(
            "inventory:1");

        Assert.Equal(10, successfulReservations);
        Assert.Equal(10, rejectedReservations);
        Assert.Equal("0", finalStock);

        await connection.CloseAsync();
    }
}