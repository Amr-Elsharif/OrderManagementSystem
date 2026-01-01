using MediatR;

namespace OrderManagementSystem.Application.Commands.Orders.RemoveOrderItem
{
    public class RemoveOrderItemCommand : IRequest<bool>
    {
        public Guid OrderId { get; init; }
        public Guid OrderItemId { get; init; }
        public string? Reason { get; init; }
        public string? RemovedBy { get; init; }
    }
}
