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

        Task SetStockAsync(
            int inventoryId,
            int stock,
            TimeSpan? expiration = null);
    }
}
