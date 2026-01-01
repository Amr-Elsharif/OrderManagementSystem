namespace OrderManagementSystem.Domain.ValueObjects
{
    public class CancelledItem(Guid productId, string productName, int quantity, decimal unitPrice, bool stockRestored)
    {
        public Guid ProductId { get; } = productId;
        public string ProductName { get; } = productName;
        public int Quantity { get; } = quantity;
        public decimal UnitPrice { get; } = unitPrice;
        public bool StockRestored { get; } = stockRestored;
    }
}
