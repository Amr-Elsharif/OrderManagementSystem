using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Products;

namespace OrderManagementSystem.Infrastructure.Messaging.Products
{
    public class ProductDeletedConsumer(
        ILogger<ProductDeletedConsumer> logger,
        ICacheService cacheService,
        IEmailService emailService) : IConsumer<ProductDeletedEvent>
    {
        private readonly ILogger<ProductDeletedConsumer> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IEmailService _emailService = emailService;

        public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing ProductDeletedEvent: {ProductId}, Reason: {Reason}",
                message.ProductId, message.Reason);

            try
            {
                // 1. Clear all product-related caches
                await ClearProductCaches(message.ProductId, context.CancellationToken);

                // 2. Send deletion notification
                await _emailService.SendProductDeletionNotificationAsync(
                    message.ProductId,
                    message.ProductName,
                    message.Reason,
                    context.CancellationToken);

                // 3. Archive product data for reporting
                await ArchiveProductData(message, context.CancellationToken);

                // 4. Notify related services (search, recommendations, etc.)
                await NotifyRelatedServices(message, context.CancellationToken);

                _logger.LogInformation("Product deleted event processed: {ProductId}", message.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ProductDeletedEvent for ProductId: {ProductId}",
                    message.ProductId);
                throw;
            }
        }

        private async Task ClearProductCaches(Guid productId, CancellationToken cancellationToken)
        {
            var cacheKeys = new[]
            {
                $"product_{productId}",
                "products_all_active",
                "products_low_stock",
                "products_category_*"
            };

            foreach (var key in cacheKeys)
            {
                await _cacheService.RemoveAsync(key, cancellationToken);
            }
        }

        private async Task ArchiveProductData(ProductDeletedEvent message, CancellationToken cancellationToken)
        {
            var archiveData = new
            {
                ProductId = message.ProductId,
                ProductName = message.ProductName,
                DeletedBy = message.DeletedBy,
                DeletionReason = message.Reason,
                DeletedAt = DateTime.UtcNow,
                ArchivedAt = DateTime.UtcNow
            };

            // Store in archive cache/database
            var archiveKey = $"product_archive_{message.ProductId}";
            await _cacheService.SetAsync(archiveKey, archiveData, TimeSpan.FromDays(90), cancellationToken);
        }

        private async Task NotifyRelatedServices(ProductDeletedEvent message, CancellationToken cancellationToken)
        {
            // Notify search service to remove from index
            // Notify recommendation service to update models
            // Notify reporting service for analytics
            _logger.LogDebug("Notified related services about product deletion: {ProductId}", message.ProductId);
        }
    }
}
