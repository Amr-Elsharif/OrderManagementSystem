using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Products
{
    public class ProductCreatedEvent(Guid productId, string productName, string sku,
                             decimal price, string category, int initialStock, string createdBy) : BaseEvent
    {
        public Guid ProductId { get; } = productId;
        public string ProductName { get; } = productName;
        public string Sku { get; } = sku;
        public decimal Price { get; } = price;
        public string Category { get; } = category;
        public int InitialStock { get; } = initialStock;
        public string CreatedBy { get; } = createdBy;
    }
}
