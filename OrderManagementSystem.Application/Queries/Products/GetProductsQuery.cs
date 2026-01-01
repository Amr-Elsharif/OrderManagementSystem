using MediatR;
using OrderManagementSystem.Application.DTOs.Common;
using OrderManagementSystem.Application.DTOs.Products;

namespace OrderManagementSystem.Application.Queries.Products
{
    public class GetProductsQuery : IRequest<PaginatedResult<ProductDto>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string? Category { get; init; }
        public bool? IsActive { get; init; } = true;
        public string? SearchTerm { get; init; }
        public decimal? MinPrice { get; init; }
        public decimal? MaxPrice { get; init; }
        public string? SortBy { get; init; } = "name";
        public bool SortDescending { get; init; } = false;
    }
}