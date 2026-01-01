using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Orders
{
    public class OrderItemQuantityUpdatedEvent(Guid orderId, Guid orderItemId, Guid productId,
                                       int oldQuantity, int newQuantity, string updatedBy) : BaseEvent
    {
        public Guid OrderId { get; } = orderId;
        public Guid OrderItemId { get; } = orderItemId;
        public Guid ProductId { get; } = productId;
        public int OldQuantity { get; } = oldQuantity;
        public int NewQuantity { get; } = newQuantity;
        public string UpdatedBy { get; } = updatedBy;
    }

}
