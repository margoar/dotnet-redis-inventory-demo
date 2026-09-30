using RedisInventoryDemo.Application.Abstractions.Caching;
using RedisInventoryDemo.Application.Abstractions.Concurrency;
using RedisInventoryDemo.Application.Abstractions.Persistence;
using RedisInventoryDemo.Application.Contracts.Inventory;
using RedisInventoryDemo.Application.Services;
using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Tests.Services;

public class InventoryServiceTests
{
    
    internal sealed class FakeInventoryCache : IInventoryCache
    {
        private readonly int? _cachedStock;
        public FakeInventoryCache(int? cachedStock = 10)
        {
            _cachedStock = cachedStock;
        }

        public Task<int?> GetStockAsync(int inventoryId)
        {
            return Task.FromResult(_cachedStock);
        }

        public Task SetStockAsync( int inventoryId, int stock,  TimeSpan? expiration = null)
        {
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly InventoryItem? _item;

        public FakeInventoryRepository(
            bool inventoryExists = true)
        {
            _item = inventoryExists
                ? new InventoryItem
                {
                    Id = 1,
                    Name = "Producto 1",
                    Stock = 10
                }
                : null;
        }

        public Task<InventoryItem?> GetByIdAsync(int id)
        {
            return Task.FromResult(_item);
        }
    }


    internal sealed class FakeInventoryStockStore : IInventoryStockStore
    {
        private readonly ReserveStockResult _reserveResult;

        public FakeInventoryStockStore(ReserveStockResult? reserveResult = null)
        {
            _reserveResult = reserveResult  ?? new ReserveStockResult(ReserveStockStatus.Reserved, 7);
        }

        public Task<ReserveStockResult> TryReserveStockAsync(int inventoryId,int quantity)
        {
            return Task.FromResult(_reserveResult);
        }
    }

    [Fact]
    public async Task ReserveAsync_ShouldReturnReservedStock()
    {
        // Arrange
        var cache = new FakeInventoryCache();
        var stockStore = new FakeInventoryStockStore();
        var service = new InventoryService(new FakeInventoryRepository(), cache, stockStore);

        // Act
        var result = await service.ReserveAsync(1, new ReserveInventoryRequest(3));

        // Assert
        Assert.Equal(ReserveStockStatus.Reserved, result.Status);

        Assert.Equal(7, result.RemainingStock);
    }

    [Fact]
    public async Task ReserveAsync_ShouldReturnInsufficientStock()
    {
        var cache = new FakeInventoryCache(); 
        var stockStore = new FakeInventoryStockStore(new ReserveStockResult( ReserveStockStatus.InsufficientStock,  null));
        var service = new InventoryService(new FakeInventoryRepository(), cache, stockStore);
        var result = await service.ReserveAsync(1,new ReserveInventoryRequest(11));



        Assert.Equal(ReserveStockStatus.InsufficientStock, result.Status);
        Assert.Null(result.RemainingStock);
    }


    [Fact]
    public async Task ReserveAsync_ShouldReturnInventoryNotFound()
    {
        var cache = new FakeInventoryCache(cachedStock: null);
        var repository = new FakeInventoryRepository( inventoryExists: false);
        var stockStore = new FakeInventoryStockStore();

        var service = new InventoryService(repository, cache,stockStore);

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
        var stockStore = new FakeInventoryStockStore();

        var service = new InventoryService(repository, cache, stockStore);
        var request = new ReserveInventoryRequest(0);

        // Act
        var result = await service.ReserveAsync(1, request);

        // Assert
        Assert.Equal( ReserveStockStatus.InvalidQuantity, result.Status);
        Assert.Null(result.RemainingStock);
    }
}