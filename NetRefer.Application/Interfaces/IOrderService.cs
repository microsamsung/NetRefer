using NetRefer.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetRefer.Application.Interfaces
{
    public interface IOrderService
    {
        Task<int> CreateOrderAsync(int customerId, List<OrderItemViewModel> items);
    }
}
