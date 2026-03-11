using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetRefer.Application
{
    public record CreateOrderCommand(string CustomerName, decimal Amount);
}
