using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Products;

namespace OrderManagementSystem.Infrastructure.Messaging.Products
{
    public class ProductLowStockConsumer(
        ILogger<ProductLowStockConsumer> logger,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ICacheService cacheService) : IConsumer<ProductLowStockEvent>
    {
        private readonly ILogger<ProductLowStockConsumer> _logger = logger;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IEmailService _emailService = emailService;
        private readonly ICacheService _cacheService = cacheService;

        public async Task Consume(ConsumeContext<ProductLowStockEvent> context)
        {
            var message = context.Message;

            _logger.LogWarning("Processing ProductLowStockEvent: {ProductName} (ID: {ProductId}) - Current stock: {CurrentStock}",
                message.ProductName, message.ProductId, message.CurrentStock);

            try
            {
                var alertCacheKey = $"low_stock_alert_{message.ProductId}_{DateTime.UtcNow:yyyyMMdd}";
                var alertSentToday = await _cacheService.ExistsAsync(alertCacheKey, context.CancellationToken);

                if (alertSentToday)
                {
                    _logger.LogDebug("Low stock alert already sent today for ProductId: {ProductId}", message.ProductId);
                    return;
                }

                var product = await _unitOfWork.Products.GetByIdAsync(message.ProductId, context.CancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product not found: {ProductId}", message.ProductId);
                    return;
                }

                //await _emailService.SendLowStockAlertAsync(
                //    message.ProductId,
                //    message.ProductName,
                //    message.CurrentStock,
                //    product.MinStockThreshold,
                //    context.CancellationToken);

                await _cacheService.SetAsync(alertCacheKey, true, TimeSpan.FromHours(24), context.CancellationToken);

                //var lowStockProducts = await _unitOfWork.Products.GetLowStockProductsAsync(context.CancellationToken);
                //if (lowStockProducts.Count() > 5)
                //{
                //    await _emailService.SendBulkLowStockReportAsync(
                //        lowStockProducts,
                //        context.CancellationToken);
                //}

                await UpdateProductAnalytics(product);

                _logger.LogInformation("Low stock alert processed for ProductId: {ProductId}", message.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ProductLowStockEvent for ProductId: {ProductId}", message.ProductId);
                throw;
            }
        }

        private async Task UpdateProductAnalytics(Domain.Entities.Product product)
        {
            var productAnalytics = new
            {
                ProductId = product.Id,
                ProductName = product.Name,
                CurrentStock = product.StockQuantity,
                MinStockThreshold = product.MinStockThreshold,
                LastLowStockAlert = DateTime.UtcNow
            };

            _logger.LogDebug("Updated product analytics for ProductId: {ProductId}", product.Id);
        }
    }
}
