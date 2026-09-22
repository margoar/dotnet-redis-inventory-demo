namespace RedisInventoryDemo.Application.Contracts.Inventory;

public enum ReserveStockStatus
{
    InventoryNotFound,
    InsufficientStock,
    Reserved
}

public sealed record ReserveStockResult( ReserveStockStatus Status, int? RemainingStock);