using Microsoft.EntityFrameworkCore;
using NetRefer.Application.Interfaces;
using NetRefer.Domain.Entities;
using NetRefer.Infrastructure.Data;

namespace NetRefer.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<int> CreateAsync(Order order)
    {
        _context.Orders.Add(order);

        await _context.SaveChangesAsync();

        return order.Id;
    }
}