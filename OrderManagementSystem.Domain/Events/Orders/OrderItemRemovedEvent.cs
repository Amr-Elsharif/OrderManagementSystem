using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Orders
{
    public class OrderItemRemovedEvent(Guid orderId, Guid orderItemId, Guid productId,
                                int quantity, string reason, string removedBy) : BaseEvent
    {
        public Guid OrderId { get; } = orderId;
        public Guid OrderItemId { get; } = orderItemId;
        public Guid ProductId { get; } = productId;
        public int Quantity { get; } = quantity;
        public string Reason { get; } = reason;
        public string RemovedBy { get; } = removedBy;
    }
}
