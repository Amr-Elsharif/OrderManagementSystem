using MediatR;

namespace OrderManagementSystem.Application.Commands.Orders.UpdateOrderItemQuantity
{
    public class UpdateOrderItemQuantityCommand : IRequest<bool>
    {
        public Guid OrderId { get; init; }
        public Guid OrderItemId { get; init; }
        public int NewQuantity { get; init; }
        public string? UpdatedBy { get; init; }
    }
}
