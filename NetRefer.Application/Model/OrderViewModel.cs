using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetRefer.Application.Model
{
    public class OrderViewModel
    {
        [Required]
        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; }
    }
}
