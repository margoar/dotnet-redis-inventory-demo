using RedisInventoryDemo.Application.Abstractions.Persistence;
using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<InventoryItem?> GetByIdAsync(int id)
    {
        return await _inventoryRepository.GetByIdAsync(id);
    }
}