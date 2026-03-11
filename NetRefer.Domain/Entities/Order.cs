using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetRefer.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
