using MediatR;

namespace OrderManagementSystem.Application.Commands.Products.CreateProduct
{
    public class CreateProductCommand : IRequest<Guid>
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Sku { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int StockQuantity { get; init; }
        public string Category { get; init; } = string.Empty;
        public string CreatedBy { get; init; } = string.Empty;
    }
}