namespace RedisInventoryDemo.Application.Contracts.Inventory;

public sealed record ReserveInventoryRequest(
    int Quantity);