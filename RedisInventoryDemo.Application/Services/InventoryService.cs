using RedisInventoryDemo.Application.Abstractions.Caching;
using RedisInventoryDemo.Application.Abstractions.Persistence;
using RedisInventoryDemo.Application.Contracts.Inventory;
using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryCache _inventoryCache;


    public InventoryService(IInventoryRepository inventoryRepository , IInventoryCache inventoryCache)
    {
        _inventoryRepository = inventoryRepository;
        _inventoryCache = inventoryCache;
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

        await _inventoryCache.SetStockAsync(
            item.Id,
            item.Stock,
            TimeSpan.FromMinutes(10));

        return item;
    }

    public async Task<InventoryItem?> ReserveAsync(int id,ReserveInventoryRequest request)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException(
                "La cantidad debe ser mayor que cero.");

        var cachedStock = await _inventoryCache.GetStockAsync(id);

        if (!cachedStock.HasValue)
        {
            var item = await _inventoryRepository.GetByIdAsync(id);

            if (item is null)
                return null;

            cachedStock = item.Stock;

            await _inventoryCache.SetStockAsync(
                id,
                cachedStock.Value,
                TimeSpan.FromMinutes(10));
        }

        if (cachedStock.Value < request.Quantity)
            return null;

        var newStock = cachedStock.Value - request.Quantity;

        await _inventoryCache.SetStockAsync(
            id,
            newStock,
            TimeSpan.FromMinutes(10));

        return new InventoryItem
        {
            Id = id,
            Name = $"Producto {id}",
            Stock = newStock
        };
    }
}