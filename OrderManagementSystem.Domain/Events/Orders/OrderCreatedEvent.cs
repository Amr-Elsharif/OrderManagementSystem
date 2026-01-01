using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Orders
{
    public class OrderCreatedEvent(Guid orderId, Guid customerId, string orderNumber) : BaseEvent
    {
        public Guid OrderId { get; } = orderId;
        public Guid CustomerId { get; } = customerId;
        public string OrderNumber { get; } = orderNumber;
    }
}