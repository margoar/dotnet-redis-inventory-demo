using RedisInventoryDemo.Application.Abstractions.Caching;
using RedisInventoryDemo.Application.Abstractions.Persistence;
using RedisInventoryDemo.Application.Contracts.Inventory;
using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryCache _inventoryCache;
    private readonly IInventoryStockStore _inventoryStockStore;


    public InventoryService(IInventoryRepository inventoryRepository , IInventoryCache inventoryCache, IInventoryStockStore inventoryStockStore)
    {
        _inventoryRepository = inventoryRepository;
        _inventoryCache = inventoryCache;
        _inventoryStockStore = inventoryStockStore;
    }

    public async Task<InventoryItem?> GetByIdAsync(int id)
    {
        var cachedStock = await _inventoryCache.GetStockAsync(id);

        if (cachedStock.HasValue)
        {
            return new InventoryItem
            {
                Id = id,
                Name = $"Producto {id}",
                Stock = cachedStock.Value
            };
        }

        var item = await _inventoryRepository.GetByIdAsync(id);

        if (item is null)
            return null;

        await _inventoryCache.SetStockAsync(item.Id,item.Stock,TimeSpan.FromMinutes(10));

        return item;
    }

    public async Task<ReserveStockResult> ReserveAsync(int id, ReserveInventoryRequest request)
    {

        if (request.Quantity <= 0)
        {
            return new ReserveStockResult(ReserveStockStatus.InvalidQuantity, null);
        }

        var cachedStock = await _inventoryCache.GetStockAsync(id);

        if (!cachedStock.HasValue)
        {
            var item = await _inventoryRepository.GetByIdAsync(id);

            if (item is null)
            {
                return new ReserveStockResult(ReserveStockStatus.InventoryNotFound,null);
            }

            await _inventoryCache.SetStockAsync(id, item.Stock, TimeSpan.FromMinutes(10));
        }

        var result = await _inventoryStockStore.TryReserveStockAsync(id, request.Quantity);

        return result;
    }
}