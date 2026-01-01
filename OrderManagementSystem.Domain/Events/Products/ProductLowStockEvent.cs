using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Products
{
    public class ProductLowStockEvent(Guid productId, string productName, int currentStock) : BaseEvent
    {
        public Guid ProductId { get; } = productId;
        public string ProductName { get; } = productName;
        public int CurrentStock { get; } = currentStock;
    }
}
