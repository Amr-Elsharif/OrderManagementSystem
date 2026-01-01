using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Commands.Products.DeleteProduct;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Products;

namespace OrderManagementSystem.Application.Commands.Products
{
    public class DeleteProductCommandHandler(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        ILogger<DeleteProductCommandHandler> logger,
        ICacheService cacheService) : IRequestHandler<DeleteProductCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMessagePublisher _messagePublisher = messagePublisher;
        private readonly ILogger<DeleteProductCommandHandler> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
                if (product == null)
                {
                    throw new NotFoundException($"Product with ID {request.ProductId} not found");
                }

                var hasActiveOrders = await _unitOfWork.Orders.HasActiveOrdersForProductAsync(request.ProductId, cancellationToken);
                if (hasActiveOrders)
                {
                    // Get detailed count for error message
                    var activeOrderCount = await _unitOfWork.Orders.GetActiveOrderCountForProductAsync(request.ProductId, cancellationToken);

                    throw new BusinessRuleException(
                        $"Cannot delete product '{product.Name}' because it has {activeOrderCount} active order(s). " +
                        "Consider deactivating the product instead or wait until all orders are completed.");
                }

                // Get product details for event before deletion
                var productName = product.Name;
                var wasActive = product.IsActive;
                var finalStock = product.StockQuantity;

                if (request.SoftDelete)
                {
                    product.Deactivate();
                    if (!string.IsNullOrEmpty(request.DeletedBy))
                    {
                        product.UpdateTimestamps(request.DeletedBy);
                    }
                    await _unitOfWork.Products.UpdateAsync(product, cancellationToken);

                    _logger.LogInformation("Product deactivated: {ProductId}, Name: {Name}",
                        product.Id, product.Name);
                }
                else
                {
                    // Hard delete - remove from database
                    await _unitOfWork.Products.DeleteAsync(product, cancellationToken);

                    _logger.LogInformation("Product permanently deleted: {ProductId}, Name: {Name}",
                        product.Id, product.Name);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish domain event
                await _messagePublisher.PublishAsync(new ProductDeletedEvent(
                    product.Id,
                    productName,
                    request.SoftDelete ? "Soft delete (deactivated)" : "Hard delete (permanent)",
                    request.DeletedBy ?? "system",
                    wasActive,
                    finalStock), cancellationToken);

                await ClearProductCaches(product.Id, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error deleting product: {ProductId}", request.ProductId);
                throw;
            }
        }

        private async Task ClearProductCaches(Guid productId, CancellationToken cancellationToken)
        {
            var cacheKeys = new[]
            {
                $"product_{productId}",
                $"product_sku_*",
                "products_all_active",
                "products_low_stock",
                $"products_category_*"
            };

            foreach (var key in cacheKeys)
            {
                try
                {
                    await _cacheService.RemoveAsync(key, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clear cache key: {Key}", key);
                }
            }
        }
    }
}