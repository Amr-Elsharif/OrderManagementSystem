namespace OrderManagementSystem.Domain.ValueObjects
{
    public class ShippedItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        public Guid ProductId { get; } = productId;
        public string ProductName { get; } = productName;
        public int Quantity { get; } = quantity;
        public decimal UnitPrice { get; } = unitPrice;
    }
}
