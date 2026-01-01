namespace OrderManagementSystem.Application.DTOs.Orders
{
    public class CreateOrderCommandDto
    {
        public Guid CustomerId { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = [];
        public string? ShippingAddress { get; set; }
        public string? Notes { get; set; }
    }
}
