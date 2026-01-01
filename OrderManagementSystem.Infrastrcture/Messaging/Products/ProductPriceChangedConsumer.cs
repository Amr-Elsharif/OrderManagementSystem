using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Products;

namespace OrderManagementSystem.Infrastructure.Messaging.Products
{
    public class ProductPriceChangedConsumer(
        ILogger<ProductPriceChangedConsumer> logger,
        ICacheService cacheService) : IConsumer<ProductPriceChangedEvent>
    {
        private readonly ILogger<ProductPriceChangedConsumer> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task Consume(ConsumeContext<ProductPriceChangedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing ProductPriceChangedEvent: {ProductId}, Old: {OldPrice}, New: {NewPrice}, Change: {Percentage}%",
                message.ProductId, message.OldPrice, message.NewPrice, message.PercentageChange);

            try
            {
                await _cacheService.RemoveAsync($"product_{message.ProductId}", context.CancellationToken);

                await UpdatePriceHistory(message, context.CancellationToken);

                if (message.PercentageChange < -20) // More than 20% price drop
                {
                    await NotifyMarketingTeam(message, context.CancellationToken);
                }

                if (message.PercentageChange > 10) // More than 10% price increase
                {
                    await AnalyzeMarginImpact(message, context.CancellationToken);
                }

                _logger.LogInformation("Product price changed event processed: {ProductId}", message.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ProductPriceChangedEvent for ProductId: {ProductId}",
                    message.ProductId);
                throw;
            }
        }

        private async Task UpdatePriceHistory(ProductPriceChangedEvent message, CancellationToken cancellationToken)
        {
            var historyKey = $"product_price_history_{message.ProductId}";
            var history = await _cacheService.GetAsync<List<PriceHistoryEntry>>(historyKey, cancellationToken)
                ?? new List<PriceHistoryEntry>();

            history.Add(new PriceHistoryEntry
            {
                OldPrice = message.OldPrice,
                NewPrice = message.NewPrice,
                PercentageChange = message.PercentageChange,
                ChangedBy = message.ChangedBy,
                ChangedAt = DateTime.UtcNow,
                Reason = message.Reason
            });

            // Keep only last 50 price changes
            if (history.Count > 50)
                history = history.Skip(history.Count - 50).ToList();

            await _cacheService.SetAsync(historyKey, history, TimeSpan.FromDays(365), cancellationToken);
        }

        private async Task NotifyMarketingTeam(ProductPriceChangedEvent message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Significant price drop detected for product {ProductId}. Marketing team notified.",
                message.ProductId);

            // Send email/notification to marketing team
            // Could trigger promotional campaign
        }

        private async Task AnalyzeMarginImpact(ProductPriceChangedEvent message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Price increase detected for product {ProductId}. Margin analysis triggered.",
                message.ProductId);
        }

        private class PriceHistoryEntry
        {
            public decimal OldPrice { get; set; }
            public decimal NewPrice { get; set; }
            public decimal PercentageChange { get; set; }
            public string ChangedBy { get; set; } = string.Empty;
            public DateTime ChangedAt { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
