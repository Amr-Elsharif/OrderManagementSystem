using MediatR;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Commands.Orders.UpdateOrder
{
    public class UpdateOrderStatusCommand : IRequest<bool>
    {
        public Guid OrderId { get; init; }
        public OrderStatus Status { get; init; }
        public string? Notes { get; init; }
        public string? UpdatedBy { get; init; }
    }
}