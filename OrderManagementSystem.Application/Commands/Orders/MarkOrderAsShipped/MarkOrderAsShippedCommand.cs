using MediatR;

namespace OrderManagementSystem.Application.Commands.Orders.MarkOrderAsShipped
{
    public class MarkOrderAsShippedCommand : IRequest<bool>
    {
        public Guid OrderId { get; init; }
        public string TrackingNumber { get; init; } = string.Empty;
        public string? ShippedBy { get; init; }
    }
}
