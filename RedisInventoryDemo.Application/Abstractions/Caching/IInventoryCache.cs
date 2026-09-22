using RedisInventoryDemo.Application.Contracts.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisInventoryDemo.Application.Abstractions.Caching
{
    public interface IInventoryCache
    {
        Task<int?> GetStockAsync(int inventoryId);

        Task SetStockAsync( int inventoryId, int stock,  TimeSpan? expiration = null);

        Task<ReserveStockResult> TryReserveStockAsync(int inventoryId,  int quantity);


    }
}
