using RedisInventoryDemo.Application.Abstractions.Persistence;
using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Infrastructure.Persistence;

public sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly List<InventoryItem> _items =
    [
        new InventoryItem
        {
            Id = 1,
            Name = "Producto 1",
            Stock = 10
        },
        new InventoryItem
        {
            Id = 2,
            Name = "Producto 2",
            Stock = 20
        }
    ];

    public Task<InventoryItem?> GetByIdAsync(int id)
    {
        var item = _items.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(item);
    }
}