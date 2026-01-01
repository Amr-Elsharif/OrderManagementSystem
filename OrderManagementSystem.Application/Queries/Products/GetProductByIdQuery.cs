using MediatR;
using OrderManagementSystem.Application.DTOs.Products;

namespace OrderManagementSystem.Application.Queries.Products
{
    public class GetProductByIdQuery : IRequest<ProductDto>
    {
        public Guid ProductId { get; init; }
    }
}