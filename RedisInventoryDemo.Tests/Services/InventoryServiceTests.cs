using RedisInventoryDemo.Application.Abstractions.Caching;
using RedisInventoryDemo.Application.Abstractions.Persistence;
using RedisInventoryDemo.Application.Contracts.Inventory;
using RedisInventoryDemo.Application.Services;
using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Tests.Services;

public class InventoryServiceTests
{
    [Fact]
    public async Task ReserveAsync_ShouldReturnReservedStock()
    {
        // Arrange
        var cache = new FakeInventoryCache();
        var service = new InventoryService(
            new FakeInventoryRepository(),
            cache);

        // Act
        var result = await service.ReserveAsync(
            1,
            new ReserveInventoryRequest(3));

        // Assert
        Assert.Equal(
            ReserveStockStatus.Reserved,
            result.Status);

        Assert.Equal(
            7,
            result.RemainingStock);
    }

    internal sealed class FakeInventoryCache : IInventoryCache
    {
        public Task<int?> GetStockAsync(int inventoryId)
            => Task.FromResult<int?>(10);

        public Task SetStockAsync(
            int inventoryId,
            int stock,
            TimeSpan? expiration = null)
            => Task.CompletedTask;

        public Task<ReserveStockResult> TryReserveStockAsync(
            int inventoryId,
            int quantity)
        {
            return Task.FromResult(
                new ReserveStockResult(
                    ReserveStockStatus.Reserved,
                    10 - quantity));
        }
    }

    internal sealed class FakeInventoryRepository : IInventoryRepository
    {
        public Task<InventoryItem?> GetByIdAsync(int id)
        {
            return Task.FromResult<InventoryItem?>(
                new InventoryItem
                {
                    Id = id,
                    Name = $"Producto {id}",
                    Stock = 10
                });
        }
    }
}