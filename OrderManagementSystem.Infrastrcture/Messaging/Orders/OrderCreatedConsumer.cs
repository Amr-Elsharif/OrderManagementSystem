using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Infrastructure.Messaging
{
    public class OrderCreatedConsumer(
        ILogger<OrderCreatedConsumer> logger,
        IEmailService emailService,
        ICustomerRepository customerRepository,
        IOrderRepository orderRepository) : IConsumer<OrderCreatedEvent>
    {
        private readonly ILogger<OrderCreatedConsumer> _logger = logger;
        private readonly IEmailService _emailService = emailService;
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly IOrderRepository _orderRepository = orderRepository;

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing OrderCreatedEvent for OrderId: {OrderId}, OrderNumber: {OrderNumber}",
                message.OrderId, message.OrderNumber);

            try
            {
                var order = await _orderRepository.GetByIdAsync(message.OrderId, context.CancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", message.OrderId);
                    return;
                }

                var customer = await _customerRepository.GetByIdAsync(order.CustomerId, context.CancellationToken);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found: {CustomerId}", order.CustomerId);
                    return;
                }

                await _emailService.SendOrderConfirmationAsync(
                    message.OrderId,
                    customer.Email,
                    context.CancellationToken);

                order.UpdateTimestamps("system_consumer");
                await _orderRepository.UpdateAsync(order, context.CancellationToken);

                await UpdateCustomerStatistics(customer.Id, context.CancellationToken);

                _logger.LogInformation("Order created event processed successfully for OrderId: {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OrderCreatedEvent for OrderId: {OrderId}", message.OrderId);
                throw;
            }
        }

        private async Task UpdateCustomerStatistics(Guid customerId, CancellationToken cancellationToken)
        {
            // يمكنك تحديث إحصائيات العميل مثل عدد الطلبات، إجمالي المشتريات، إلخ
            _logger.LogDebug("Updating statistics for customer: {CustomerId}", customerId);
        }
    }
}