using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;

namespace OrderManagementSystem.Infrastructure.Messaging.Product
{
    public class RabbitMQPublisher(IPublishEndpoint publishEndpoint, ILogger<RabbitMQPublisher> logger) : IMessagePublisher
    {
        private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
        private readonly ILogger<RabbitMQPublisher> _logger = logger;

        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                await _publishEndpoint.Publish(message, cancellationToken);
                _logger.LogDebug("Message published: {MessageType}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message of type {MessageType}", typeof(T).Name);
                throw;
            }
        }
    }
}