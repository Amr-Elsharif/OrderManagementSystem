using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Products
{
    public class ProductUpdatedEvent : BaseEvent
    {
        public Guid ProductId { get; }
        public decimal? PriceChangePercentage { get; }
        public string UpdatedBy { get; }
        public DateTime UpdatedAt { get; }

        public ProductUpdatedEvent(Guid productId, decimal? priceChangePercentage, string updatedBy)
        {
            ProductId = productId;
            PriceChangePercentage = priceChangePercentage;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
