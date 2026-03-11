using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetRefer.Application.Interfaces
{
    public interface IIdempotencyService
    {
        Task<bool> ExistsAsync(string key);

        Task StoreAsync(string key, int orderId);
    }
}
