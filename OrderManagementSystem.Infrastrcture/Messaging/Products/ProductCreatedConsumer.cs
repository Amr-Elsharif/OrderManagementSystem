using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Products;

namespace OrderManagementSystem.Infrastructure.Messaging.Products
{
    public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
    {
        private readonly ILogger<ProductCreatedConsumer> _logger;
        private readonly ICacheService _cacheService;
        private readonly IEmailService _emailService;

        public ProductCreatedConsumer(
            ILogger<ProductCreatedConsumer> logger,
            ICacheService cacheService,
            IEmailService emailService)
        {
            _logger = logger;
            _cacheService = cacheService;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing ProductCreatedEvent: {ProductName} (ID: {ProductId})",
                message.ProductName, message.ProductId);

            try
            {
                // 1. Clear product cache
                await _cacheService.RemoveAsync("products_all_active", context.CancellationToken);
                await _cacheService.RemoveAsync($"products_category_{message.Category}", context.CancellationToken);

                // 2. Send notification to inventory manager for high-value products
                if (message.Price > 1000) // High-value threshold
                {
                    await _emailService.SendHighValueProductAlertAsync(
                        message.ProductId,
                        message.ProductName,
                        message.Price,
                        message.Category,
                        context.CancellationToken);
                }

                // 3. Update product catalog analytics
                await UpdateProductAnalytics(message, context.CancellationToken);

                _logger.LogInformation("Product created event processed: {ProductId}", message.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ProductCreatedEvent for ProductId: {ProductId}",
                    message.ProductId);
                throw;
            }
        }

        private async Task UpdateProductAnalytics(ProductCreatedEvent message, CancellationToken cancellationToken)
        {
            var analyticsKey = $"product_analytics_{DateTime.UtcNow:yyyyMM}";
            var analytics = await _cacheService.GetAsync<ProductAnalytics>(analyticsKey, cancellationToken)
                ?? new ProductAnalytics();

            analytics.TotalProductsCreated++;
            analytics.TotalInventoryValue += message.Price * message.InitialStock;

            // Categorize by price
            if (message.Price < 50) analytics.LowPriceProducts++;
            else if (message.Price < 200) analytics.MidPriceProducts++;
            else analytics.HighPriceProducts++;

            await _cacheService.SetAsync(analyticsKey, analytics, TimeSpan.FromDays(30), cancellationToken);
        }

        private class ProductAnalytics
        {
            public int TotalProductsCreated { get; set; }
            public decimal TotalInventoryValue { get; set; }
            public int LowPriceProducts { get; set; }
            public int MidPriceProducts { get; set; }
            public int HighPriceProducts { get; set; }
        }
    }
}
