using RedisInventoryDemo.Application.Contracts.Inventory;
using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Application.Services;

public interface IInventoryService
{
    Task<InventoryItem?> GetByIdAsync(int id);
    Task<InventoryItem?> ReserveAsync(int id, ReserveInventoryRequest request);
}