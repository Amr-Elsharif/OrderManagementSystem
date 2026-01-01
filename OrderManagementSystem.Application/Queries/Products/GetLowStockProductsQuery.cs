using MediatR;
using OrderManagementSystem.Application.DTOs.Products;

namespace OrderManagementSystem.Application.Queries.Products
{
    public class GetLowStockProductsQuery : IRequest<List<ProductDto>>
    {
        public int? Threshold { get; init; }
    }
}