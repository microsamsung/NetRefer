namespace NetRefer.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<bool> ProcessPaymentAsync(int orderId, decimal amount);
    }
}
