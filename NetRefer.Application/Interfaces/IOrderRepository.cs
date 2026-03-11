using NetRefer.Domain.Entities;

namespace NetRefer.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);

        Task<int> CreateAsync(Order order);
    }
}
