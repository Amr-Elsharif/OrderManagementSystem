using OrderManagementSystem.Domain.Common;
using OrderManagementSystem.Domain.Exceptions;

namespace OrderManagementSystem.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; private set; }
        public Order Order { get; set; }
        public Guid ProductId { get; private set; }
        public string ProductName { get; private set; } = string.Empty;
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal TotalPrice => Quantity * UnitPrice;

        private OrderItem() { }

        public OrderItem(Guid productId, string productName, int quantity, decimal unitPrice)
        {
            ProductId = productId;
            ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
            SetQuantity(quantity);
            SetUnitPrice(unitPrice);
        }

        public void UpdateQuantity(int newQuantity)
        {
            SetQuantity(newQuantity);
        }

        public void UpdateUnitPrice(decimal newUnitPrice)
        {
            SetUnitPrice(newUnitPrice);
        }

        private void SetQuantity(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than zero");

            Quantity = quantity;
        }

        private void SetUnitPrice(decimal unitPrice)
        {
            if (unitPrice <= 0)
                throw new DomainException("Unit price must be greater than zero");

            UnitPrice = unitPrice;
        }
    }
}