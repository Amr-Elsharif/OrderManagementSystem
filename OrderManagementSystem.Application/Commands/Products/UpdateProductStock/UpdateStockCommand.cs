using MediatR;

namespace OrderManagementSystem.Application.Commands.Products.UpdateProductStock
{
    public class UpdateProductStockCommand : IRequest<bool>
    {
        public Guid ProductId { get; init; }
        public int QuantityChange { get; init; } // Positive for increase, negative for decrease
        public string? Reason { get; init; }
        public string? UpdatedBy { get; init; }
    }
}