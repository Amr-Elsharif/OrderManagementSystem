using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Infrastructure.Messaging.Orders
{
    public class OrderItemRemovedConsumer(
        ILogger<OrderItemRemovedConsumer> logger,
        ICacheService cacheService) : IConsumer<OrderItemRemovedEvent>
    {
        private readonly ILogger<OrderItemRemovedConsumer> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task Consume(ConsumeContext<OrderItemRemovedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing OrderItemRemovedEvent: Order {OrderId}, Item {OrderItemId}, Product {ProductId}",
                message.OrderId, message.OrderItemId, message.ProductId);

            // Clear order cache
            await _cacheService.RemoveAsync($"order_{message.OrderId}", context.CancellationToken);

            _logger.LogDebug("Order cache cleared for OrderId: {OrderId}", message.OrderId);
        }
    }
}
