using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Products
{
    public class ProductStockUpdatedEvent(Guid productId, string productName, int quantityChange,
                                  int newStock, int reorderPoint, string updatedBy, string reason) : BaseEvent
    {
        public Guid ProductId { get; } = productId;
        public string ProductName { get; } = productName;
        public int QuantityChange { get; } = quantityChange;
        public int NewStock { get; } = newStock;
        public int ReorderPoint { get; } = reorderPoint;
        public string UpdatedBy { get; } = updatedBy;
        public string Reason { get; } = reason;
    }
}
