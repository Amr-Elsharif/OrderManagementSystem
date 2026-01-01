using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Validators.Orders
{
    public static class OrderValidationExtensions
    {
        public static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                // Valid transitions
                (OrderStatus.Pending, OrderStatus.Processing) => true,
                (OrderStatus.Pending, OrderStatus.Cancelled) => true,
                (OrderStatus.Processing, OrderStatus.Paid) => true,
                (OrderStatus.Processing, OrderStatus.Cancelled) => true,
                (OrderStatus.Paid, OrderStatus.Shipped) => true,
                (OrderStatus.Paid, OrderStatus.Cancelled) => true,
                (OrderStatus.Shipped, OrderStatus.Delivered) => true,
                (OrderStatus.Delivered, OrderStatus.Refunded) => true,

                // Invalid transitions
                _ => false
            };
        }

        public static string GetStatusTransitionError(OrderStatus currentStatus, OrderStatus newStatus)
        {
            return $"Cannot transition from {currentStatus} to {newStatus}";
        }
    }
}
