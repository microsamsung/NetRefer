using NetRefer.Application.Interfaces;
using NetRefer.Application.Model;
using NetRefer.Domain.Entities;
using NetRefer.Infrastructure.Data;

namespace NetRefer.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateOrderAsync(int customerId, List<OrderItemViewModel> items)
    {
        var order = new Order
        {
            CustomerName = $"Customer-{customerId}",
            Amount = items.Sum(i => i.Quantity * 10)
        };

        _context.Orders.Add(order);

        await _context.SaveChangesAsync();

        return order.Id;
    }
}