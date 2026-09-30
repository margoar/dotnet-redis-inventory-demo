using RedisInventoryDemo.Application.Contracts.Inventory;

public interface IInventoryStockStore
{
    Task<ReserveStockResult> TryReserveStockAsync(int inventoryId, int quantity);

}
