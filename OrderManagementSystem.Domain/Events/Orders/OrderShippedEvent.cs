using OrderManagementSystem.Domain.Common;
using OrderManagementSystem.Domain.ValueObjects;

namespace OrderManagementSystem.Domain.Events.Orders
{
    public class OrderShippedEvent(
        Guid orderId,
        string orderNumber,
        string trackingNumber,
        string shippingCarrier,
        string shippedBy,
        Guid customerId,
        List<ShippedItem> items) : BaseEvent
    {
        public Guid OrderId { get; } = orderId;
        public string OrderNumber { get; } = orderNumber;
        public string TrackingNumber { get; } = trackingNumber;
        public string ShippingCarrier { get; } = shippingCarrier;
        public string ShippedBy { get; } = shippedBy;
        public Guid CustomerId { get; } = customerId;
        public DateTime ShippedAt { get; } = DateTime.UtcNow;
        public List<ShippedItem> Items { get; } = items ?? [];
    }
}
