using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Infrastructure.Messaging
{
    public class OrderItemAddedConsumer(
        ILogger<OrderItemAddedConsumer> logger,
        ICacheService cacheService) : IConsumer<OrderItemAddedEvent>
    {
        private readonly ILogger<OrderItemAddedConsumer> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task Consume(ConsumeContext<OrderItemAddedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing OrderItemAddedEvent: Order {OrderId}, Item {OrderItemId}, Product {ProductId}, Quantity {Quantity}",
                message.OrderId, message.OrderItemId, message.ProductId, message.Quantity);

            // Clear order cache
            await _cacheService.RemoveAsync($"order_{message.OrderId}", context.CancellationToken);

            _logger.LogDebug("Order cache cleared for OrderId: {OrderId}", message.OrderId);
        }
    }
}