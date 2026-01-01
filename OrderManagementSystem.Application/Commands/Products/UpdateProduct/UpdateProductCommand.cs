using MediatR;

namespace OrderManagementSystem.Application.Commands.Products.UpdateProduct
{
    public class UpdateProductCommand : IRequest<bool>
    {
        public Guid ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string Category { get; init; } = string.Empty;
        public string? UpdatedBy { get; init; }
    }
}