using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Application.Abstractions.Persistence;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdAsync(int id);
}