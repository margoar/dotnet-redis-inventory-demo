namespace RedisInventoryDemo.Application.Contracts.Inventory;

public enum ReserveStockStatus
{
    InvalidQuantity,
    InventoryNotFound,
    InsufficientStock,
    Reserved
}
public sealed record ReserveStockResult( ReserveStockStatus Status, int? RemainingStock);