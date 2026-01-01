using MediatR;

namespace OrderManagementSystem.Application.Commands.Orders.AddOrderItem
{
    public class AddOrderItemCommand : IRequest<bool>
    {
        public Guid OrderId { get; init; }
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
        public string? AddedBy { get; init; }
    }
}
