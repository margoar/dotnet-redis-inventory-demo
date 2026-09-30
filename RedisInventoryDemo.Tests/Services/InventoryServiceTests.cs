using RedisInventoryDemo.Application.Abstractions.Caching;
using RedisInventoryDemo.Application.Abstractions.Concurrency;
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
        var service = new InventoryService(new FakeInventoryRepository(),cache);

        // Act
        var result = await service.ReserveAsync(1, new ReserveInventoryRequest(3));

        // Assert
        Assert.Equal(ReserveStockStatus.Reserved,result.Status);

        Assert.Equal(7, result.RemainingStock);
    }

    internal sealed class FakeInventoryCache : IInventoryCache
    {
        private readonly ReserveStockResult _reserveResult;

        public FakeInventoryCache( ReserveStockResult? reserveResult = null)
        {
            _reserveResult = reserveResult ?? new ReserveStockResult(ReserveStockStatus.Reserved,7);
        }

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
            => Task.FromResult(_reserveResult);
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


    [Fact]
    public async Task ReserveAsync_ShouldReturnInsufficientStock()
    {
        var cache = new FakeInventoryCache(new ReserveStockResult(ReserveStockStatus.InsufficientStock, null));
        var service = new InventoryService(new FakeInventoryRepository(), cache);
        var result = await service.ReserveAsync(1,new ReserveInventoryRequest(11));

        Assert.Equal(ReserveStockStatus.InsufficientStock, result.Status);
        Assert.Null(result.RemainingStock);
    }


    [Fact]
    public async Task ReserveAsync_ShouldReturnInventoryNotFound()
    {
        var cache = new FakeInventoryCache(new ReserveStockResult(  ReserveStockStatus.InventoryNotFound,  null));

        var service = new InventoryService(new FakeInventoryRepository(), cache);

        var result = await service.ReserveAsync(999,new ReserveInventoryRequest(1));

        Assert.Equal(ReserveStockStatus.InventoryNotFound, result.Status);
        Assert.Null(result.RemainingStock);
    }

    [Fact]
    public async Task ReserveAsync_ShouldReturnInvalidQuantity()
    {
        // Arrange
        var cache = new FakeInventoryCache();
        var repository = new FakeInventoryRepository();
        var service = new InventoryService(repository, cache);
        var request = new ReserveInventoryRequest(0);

        // Act
        var result = await service.ReserveAsync(1, request);

        // Assert
        Assert.Equal( ReserveStockStatus.InvalidQuantity, result.Status);
        Assert.Null(result.RemainingStock);
    }
}