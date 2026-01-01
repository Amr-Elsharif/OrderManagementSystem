using OrderManagementSystem.Domain.Common;
using OrderManagementSystem.Domain.Enums;
using OrderManagementSystem.Domain.Events.Orders;
using OrderManagementSystem.Domain.Exceptions;
using OrderManagementSystem.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagementSystem.Domain.Entities
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; private set; } = string.Empty;
        public Guid CustomerId { get; private set; }
        public OrderStatus Status { get; private set; }
        public Money TotalAmount { get; private set; }
        public PaymentStatus PaymentStatus { get; private set; }
        public string? ShippingAddress { get; private set; }
        public string? Notes { get; private set; }

        private readonly List<OrderItem> _items = [];

        [NotMapped]
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        private Order()
        {
            TotalAmount = Money.Zero();
        }

        public Order(Guid customerId, string createdBy = "") : base()
        {
            CustomerId = customerId;
            OrderNumber = GenerateOrderNumber();
            Status = OrderStatus.Pending;
            PaymentStatus = PaymentStatus.Pending;
            TotalAmount = Money.Zero();

            if (!string.IsNullOrEmpty(createdBy))
                CreatedBy = createdBy;

            AddDomainEvent(new OrderCreatedEvent(Id, CustomerId, OrderNumber));
        }

        public OrderItem AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
        {
            var item = new OrderItem(productId, productName, quantity, unitPrice);
            _items.Add(item);
            CalculateTotal();

            AddDomainEvent(new OrderItemAddedEvent(Id, item.Id, productId, quantity, unitPrice, "system"));

            return item; // Return the added item
        }

        public OrderItem RemoveItem(Guid itemId)
        {
            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                _items.Remove(item);
                CalculateTotal();

                AddDomainEvent(new OrderItemRemovedEvent(Id, itemId, item.ProductId,
                    item.Quantity, "Removed", "system"));

                return item; // Return the removed item
            }
            return null!;
        }

        public void UpdateQuantity(Guid itemId, int newQuantity)
        {
            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                var oldQuantity = item.Quantity;
                item.UpdateQuantity(newQuantity);
                CalculateTotal();

                AddDomainEvent(new OrderItemQuantityUpdatedEvent(Id, itemId, item.ProductId,
                    oldQuantity, newQuantity, "system"));
            }
        }

        public void MarkAsPaid(string paymentMethod, decimal amountPaid, string paidBy = null)
        {
            if (Status != OrderStatus.Pending && Status != OrderStatus.Processing)
                throw new DomainException("Only pending or processing orders can be marked as paid");

            if (amountPaid != TotalAmount.Amount)
                throw new DomainException($"Payment amount {amountPaid:C} doesn't match order total {TotalAmount.Amount:C}");

            Status = OrderStatus.Paid;
            PaymentStatus = PaymentStatus.Completed;
            UpdateTimestamps(paidBy);

            AddDomainEvent(new OrderPaidEvent(
                Id,
                OrderNumber,
                paymentMethod,
                amountPaid,
                TotalAmount.Amount,
                paidBy ?? "system",
                CustomerId));
        }

        public void MarkAsShipped(string trackingNumber, string shippedBy = null, string shippingCarrier = "Standard")
        {
            if (Status != OrderStatus.Paid)
                throw new DomainException("Only paid orders can be shipped");

            Status = OrderStatus.Shipped;
            UpdateTimestamps(shippedBy);

            var shippedItems = Items.Select(item => new ShippedItem(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice
            )).ToList();

            AddDomainEvent(new OrderShippedEvent(
                Id,
                OrderNumber,
                trackingNumber,
                shippingCarrier,
                shippedBy ?? "system",
                CustomerId,
                shippedItems));
        }

        public void CancelOrder(string reason, string cancelledBy = null, bool restoreStock = true)
        {
            if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
                throw new DomainException("Shipped or delivered orders cannot be cancelled");

            Status = OrderStatus.Cancelled;
            PaymentStatus = PaymentStatus.Refunded;
            Notes = $"Cancelled: {reason}";
            UpdateTimestamps(cancelledBy);

            var cancelledItems = Items.Select(item => new CancelledItem(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                restoreStock // Track if stock was restored
            )).ToList();

            AddDomainEvent(new OrderCancelledEvent(
                Id,
                OrderNumber,
                reason,
                cancelledBy ?? "system",
                TotalAmount.Amount,
                CustomerId,
                cancelledItems));
        }

        public void UpdateShippingAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new DomainException("Shipping address cannot be empty");

            ShippingAddress = address;
            UpdateTimestamps();
        }

        private void CalculateTotal()
        {
            var total = _items.Sum(i => i.TotalPrice);
            TotalAmount = new Money(total);
        }

        private static string GenerateOrderNumber()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpper();
            return $"ORD-{datePart}-{randomPart}";
        }
    }
}