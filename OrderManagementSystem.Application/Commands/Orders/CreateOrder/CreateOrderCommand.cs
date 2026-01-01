using MediatR;

namespace OrderManagementSystem.Application.Commands.Orders.CreateOrder
{
    public class CreateOrderCommand : IRequest<Guid>
    {
        public Guid CustomerId { get; init; }
        public List<CreateOrderItemCommand> Items { get; init; } = [];
        public string? ShippingAddress { get; init; }
        public string? Notes { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
    }
}