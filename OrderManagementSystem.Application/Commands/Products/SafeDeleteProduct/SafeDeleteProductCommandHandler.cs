using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.DTOs.Products;
using OrderManagementSystem.Application.Interfaces;

namespace OrderManagementSystem.Application.Commands.Products.SafeDeleteProduct
{
    public class SafeDeleteProductCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SafeDeleteProductCommandHandler> logger) : IRequestHandler<SafeDeleteProductCommand, SafeDeleteProductResult>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<SafeDeleteProductCommandHandler> _logger = logger;

        public async Task<SafeDeleteProductResult> Handle(SafeDeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null)
            {
                return new SafeDeleteProductResult
                {
                    CanDelete = true,
                    Message = "Product not found (already deleted or doesn't exist)"
                };
            }

            var hasActiveOrders = await _unitOfWork.Orders.HasActiveOrdersForProductAsync(request.ProductId, cancellationToken);
            var activeOrderCount = await _unitOfWork.Orders.GetActiveOrderCountForProductAsync(request.ProductId, cancellationToken);

            var result = new SafeDeleteProductResult
            {
                ActiveOrderCount = activeOrderCount,
                CanDelete = !hasActiveOrders
            };

            if (hasActiveOrders)
            {
                result.Message = $"Product has {activeOrderCount} active order(s). Cannot delete until all orders are completed.";
            }
            else
            {
                result.Message = "Product can be safely deleted (no active orders)";
            }

            // Get alternatives if requested
            if (request.SuggestAlternatives && hasActiveOrders)
            {
                var alternatives = await _unitOfWork.Products.GetAlternativeProductsAsync(
                    request.ProductId,
                    product.Category,
                    5,
                    cancellationToken);

                result.Alternatives = [.. alternatives.Select(static p => new ProductAlternativeDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Category = p.Category
                })];
            }

            return result;
        }
    }
}
