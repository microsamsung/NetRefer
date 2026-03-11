namespace NetRefer.Api.DTO
{
    public class CreateOrderRequest
    {
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
