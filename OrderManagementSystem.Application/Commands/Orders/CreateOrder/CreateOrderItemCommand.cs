namespace OrderManagementSystem.Application.Commands.Orders.CreateOrder
{
    public class CreateOrderItemCommand
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
    }
}
