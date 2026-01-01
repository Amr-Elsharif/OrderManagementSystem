using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Products;

namespace OrderManagementSystem.Infrastructure.Messaging.Products
{
    public class ProductUpdatedConsumer(
        ILogger<ProductUpdatedConsumer> logger,
        ICacheService cacheService,
        IUnitOfWork unitOfWork) : IConsumer<ProductUpdatedEvent>
    {
        private readonly ILogger<ProductUpdatedConsumer> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing ProductUpdatedEvent: {ProductId}", message.ProductId);

            try
            {
                await _cacheService.RemoveAsync($"product_{message.ProductId}", context.CancellationToken);
                await _cacheService.RemoveAsync("products_all_active", context.CancellationToken);

                if (message.PriceChangePercentage.HasValue &&
                    Math.Abs(message.PriceChangePercentage.Value) > 10) // >10% change
                {
                    await NotifyPriceChange(message, context.CancellationToken);
                }

                // Update search index if needed (for Elasticsearch/Solr)
                await UpdateSearchIndex(message.ProductId, context.CancellationToken);

                _logger.LogInformation("Product updated event processed: {ProductId}", message.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ProductUpdatedEvent for ProductId: {ProductId}",
                    message.ProductId);
                throw;
            }
        }

        private async Task NotifyPriceChange(ProductUpdatedEvent message, CancellationToken cancellationToken)
        {
            // Get customers who have this product in their wishlist
            // This would require a Wishlist service or table
            _logger.LogInformation("Significant price change for product {ProductId}: {Percentage}%",
                message.ProductId, message.PriceChangePercentage);
        }

        private async Task UpdateSearchIndex(Guid productId, CancellationToken cancellationToken)
        {
            // Integration with Elasticsearch/Solr for product search
            _logger.LogDebug("Updating search index for product: {ProductId}", productId);
        }
    }
}
