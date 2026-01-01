using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Products;

namespace OrderManagementSystem.Infrastructure.Messaging.Products
{
    public class ProductStockUpdatedConsumer(
        ILogger<ProductStockUpdatedConsumer> logger,
        ICacheService cacheService,
        IEmailService emailService) : IConsumer<ProductStockUpdatedEvent>
    {
        private readonly ILogger<ProductStockUpdatedConsumer> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IEmailService _emailService = emailService;

        public async Task Consume(ConsumeContext<ProductStockUpdatedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing ProductStockUpdatedEvent: {ProductId}, Change: {QuantityChange}, New Stock: {NewStock}",
                message.ProductId, message.QuantityChange, message.NewStock);

            try
            {
                // 1. Update stock-related caches
                await _cacheService.RemoveAsync($"product_{message.ProductId}", context.CancellationToken);
                await _cacheService.RemoveAsync("products_low_stock", context.CancellationToken);

                // 2. Check for stock-out (zero stock)
                if (message.NewStock == 0)
                {
                    await _emailService.SendStockOutAlertAsync(
                        message.ProductId,
                        message.ProductName,
                        context.CancellationToken);
                }

                // 3. Update inventory analytics
                await UpdateInventoryAnalytics(message, context.CancellationToken);

                // 4. Trigger reorder if below reorder point
                if (message.NewStock <= message.ReorderPoint)
                {
                    await TriggerReorderProcess(message, context.CancellationToken);
                }

                _logger.LogInformation("Product stock updated event processed: {ProductId}", message.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ProductStockUpdatedEvent for ProductId: {ProductId}",
                    message.ProductId);
                throw;
            }
        }

        private async Task UpdateInventoryAnalytics(ProductStockUpdatedEvent message, CancellationToken cancellationToken)
        {
            var analyticsKey = $"inventory_analytics_{DateTime.UtcNow:yyyyMMdd}";
            var analytics = await _cacheService.GetAsync<InventoryAnalytics>(analyticsKey, cancellationToken)
                ?? new InventoryAnalytics();

            analytics.TotalStockMovements++;
            analytics.TotalQuantityChanged += Math.Abs(message.QuantityChange);

            if (message.QuantityChange > 0)
                analytics.StockIncreases++;
            else if (message.QuantityChange < 0)
                analytics.StockDecreases++;

            await _cacheService.SetAsync(analyticsKey, analytics, TimeSpan.FromDays(7), cancellationToken);
        }

        private async Task TriggerReorderProcess(ProductStockUpdatedEvent message, CancellationToken cancellationToken)
        {
            _logger.LogWarning("Product {ProductName} (ID: {ProductId}) is below reorder point. Current: {CurrentStock}, Reorder: {ReorderPoint}",
                message.ProductName, message.ProductId, message.NewStock, message.ReorderPoint);

            // Create purchase order or notify procurement
            // This would integrate with a procurement system
        }

        private class InventoryAnalytics
        {
            public int TotalStockMovements { get; set; }
            public int TotalQuantityChanged { get; set; }
            public int StockIncreases { get; set; }
            public int StockDecreases { get; set; }
        }
    }
}
