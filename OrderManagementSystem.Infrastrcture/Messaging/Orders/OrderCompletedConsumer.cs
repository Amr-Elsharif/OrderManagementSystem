using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Infrastructure.Messaging.Order
{
    public class OrderCompletedConsumer(
        ILogger<OrderCompletedConsumer> logger,
        ICacheService cacheService) : IConsumer<OrderShippedEvent>
    {
        private readonly ILogger<OrderCompletedConsumer> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task Consume(ConsumeContext<OrderShippedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Updating customer statistics for OrderId: {OrderId}", message.OrderId);

            try
            {
                var orderCacheKey = $"order_{message.OrderId}";
                var order = await _cacheService.GetAsync<Domain.Entities.Order>(orderCacheKey, context.CancellationToken);

                if (order != null)
                {
                    await UpdateCustomerOrderStatistics(order.CustomerId, order.TotalAmount.Amount, context.CancellationToken);
                }

                _logger.LogDebug("Customer statistics updated for order: {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer statistics for OrderId: {OrderId}", message.OrderId);
            }
        }

        private async Task UpdateCustomerOrderStatistics(Guid customerId, decimal orderAmount, CancellationToken cancellationToken)
        {
            var statsCacheKey = $"customer_stats_{customerId}";
            var customerStats = await _cacheService.GetAsync<CustomerStatistics>(statsCacheKey, cancellationToken)
                ?? new CustomerStatistics { CustomerId = customerId };

            customerStats.TotalOrders++;
            customerStats.TotalSpent += orderAmount;
            customerStats.LastOrderDate = DateTime.UtcNow;

            await _cacheService.SetAsync(statsCacheKey, customerStats, TimeSpan.FromDays(30), cancellationToken);
        }

        private class CustomerStatistics
        {
            public Guid CustomerId { get; set; }
            public int TotalOrders { get; set; }
            public decimal TotalSpent { get; set; }
            public DateTime LastOrderDate { get; set; }
        }
    }
}
