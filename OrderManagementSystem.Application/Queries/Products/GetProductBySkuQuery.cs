using MediatR;
using OrderManagementSystem.Application.DTOs.Products;

namespace OrderManagementSystem.Application.Queries.Products
{
    public class GetProductBySkuQuery : IRequest<ProductDto>
    {
        public string Sku { get; init; } = string.Empty;
    }
}