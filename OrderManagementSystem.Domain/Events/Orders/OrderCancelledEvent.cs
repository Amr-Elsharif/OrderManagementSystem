using OrderManagementSystem.Domain.Common;
using OrderManagementSystem.Domain.ValueObjects;

namespace OrderManagementSystem.Domain.Events.Orders
{
    public class OrderCancelledEvent(
        Guid orderId,
        string orderNumber,
        string reason,
        string cancelledBy,
        decimal orderTotal,
        Guid customerId,
        List<CancelledItem> items) : BaseEvent
    {
        public Guid OrderId { get; } = orderId;
        public string OrderNumber { get; } = orderNumber;
        public string Reason { get; } = reason;
        public string CancelledBy { get; } = cancelledBy;
        public decimal OrderTotal { get; } = orderTotal;
        public Guid CustomerId { get; } = customerId;
        public DateTime CancelledAt { get; } = DateTime.UtcNow;
        public List<CancelledItem> Items { get; } = items ?? [];
    }
}
