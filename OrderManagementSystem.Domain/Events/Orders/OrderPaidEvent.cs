using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Orders
{
    public class OrderPaidEvent(
        Guid orderId,
        string orderNumber,
        string paymentMethod,
        decimal amountPaid,
        decimal orderTotal,
        string paidBy,
        Guid customerId) : BaseEvent
    {
        public Guid OrderId { get; } = orderId;
        public string OrderNumber { get; } = orderNumber;
        public string PaymentMethod { get; } = paymentMethod;
        public decimal AmountPaid { get; } = amountPaid;
        public decimal OrderTotal { get; } = orderTotal;
        public string PaidBy { get; } = paidBy;
        public Guid CustomerId { get; } = customerId;
        public DateTime PaidAt { get; } = DateTime.UtcNow;
    }
}
