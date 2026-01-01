using OrderManagementSystem.Domain.Common;

namespace OrderManagementSystem.Domain.Events.Products
{
    public class ProductDeletedEvent(Guid productId, string productName, string reason,
                             string deletedBy, bool wasActive, int finalStock) : BaseEvent
    {
        public Guid ProductId { get; } = productId;
        public string ProductName { get; } = productName;
        public string Reason { get; } = reason;
        public string DeletedBy { get; } = deletedBy;
        public bool WasActive { get; } = wasActive;
        public int FinalStock { get; } = finalStock;
    }
}
