using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Orders
{
    public class OrderItemAddedEvent(Guid orderId, Guid orderItemId, Guid productId,
                              int quantity, decimal unitPrice, string addedBy) : BaseEvent
    {
        public Guid OrderId { get; } = orderId;
        public Guid OrderItemId { get; } = orderItemId;
        public Guid ProductId { get; } = productId;
        public int Quantity { get; } = quantity;
        public decimal UnitPrice { get; } = unitPrice;
        public string AddedBy { get; } = addedBy;
    }
}
