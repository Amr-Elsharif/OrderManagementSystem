using MediatR;

namespace OrderManagementSystem.Application.Commands.Orders.CancelOrder
{
    public class CancelOrderCommand : IRequest<bool>
    {
        public Guid OrderId { get; init; }
        public string Reason { get; init; } = string.Empty;
        public string? CancelledBy { get; init; }
    }
}
