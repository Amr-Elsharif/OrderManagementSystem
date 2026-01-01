using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Infrastructure.Messaging.Orders
{
    public class OrderItemQuantityUpdatedConsumer(
        ILogger<OrderItemQuantityUpdatedConsumer> logger,
        ICacheService cacheService) : IConsumer<OrderItemQuantityUpdatedEvent>
    {
        private readonly ILogger<OrderItemQuantityUpdatedConsumer> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task Consume(ConsumeContext<OrderItemQuantityUpdatedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing OrderItemQuantityUpdatedEvent: Order {OrderId}, Item {OrderItemId}, Quantity {OldQuantity} → {NewQuantity}",
                message.OrderId, message.OrderItemId, message.OldQuantity, message.NewQuantity);

            // Clear order cache
            await _cacheService.RemoveAsync($"order_{message.OrderId}", context.CancellationToken);

            _logger.LogDebug("Order cache cleared for OrderId: {OrderId}", message.OrderId);
        }
    }
}
