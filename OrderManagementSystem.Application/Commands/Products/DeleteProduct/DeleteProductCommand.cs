using MediatR;

namespace OrderManagementSystem.Application.Commands.Products.DeleteProduct
{
    public class DeleteProductCommand : IRequest<bool>
    {
        public Guid ProductId { get; init; }
        public bool SoftDelete { get; init; } = true;
        public string? DeletedBy { get; init; }
    }
}