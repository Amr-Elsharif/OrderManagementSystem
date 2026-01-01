using MediatR;
using OrderManagementSystem.Application.DTOs.Products;

namespace OrderManagementSystem.Application.Commands.Products.SafeDeleteProduct
{
    public class SafeDeleteProductCommand : IRequest<SafeDeleteProductResult>
    {
        public Guid ProductId { get; init; }
        public string Reason { get; init; } = string.Empty;
        public bool SuggestAlternatives { get; init; } = true;
        public string? DeletedBy { get; init; }
    }
}
