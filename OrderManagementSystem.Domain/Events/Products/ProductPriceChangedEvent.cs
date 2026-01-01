using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Products
{
    public class ProductPriceChangedEvent(Guid productId, decimal oldPrice, decimal newPrice,
                                  string changedBy, string reason) : BaseEvent
    {
        public Guid ProductId { get; } = productId;
        public decimal OldPrice { get; } = oldPrice;
        public decimal NewPrice { get; } = newPrice;
        public decimal PercentageChange { get; } = CalculatePercentageChange(oldPrice, newPrice);
        public string ChangedBy { get; } = changedBy;
        public string Reason { get; } = reason;

        private static decimal CalculatePercentageChange(decimal oldPrice, decimal newPrice)
        {
            if (oldPrice == 0) return 0;
            return ((newPrice - oldPrice) / oldPrice) * 100;
        }
    }
}
