using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Application.Services;

public interface IInventoryService
{
    Task<InventoryItem?> GetByIdAsync(int id);
}