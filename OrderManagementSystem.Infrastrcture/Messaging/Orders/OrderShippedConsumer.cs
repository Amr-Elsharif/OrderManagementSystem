using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Enums;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Infrastructure.Messaging.Order
{
    public class OrderShippedConsumer(
        ILogger<OrderShippedConsumer> logger,
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork) : IConsumer<OrderShippedEvent>
    {
        private readonly ILogger<OrderShippedConsumer> _logger = logger;
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly IEmailService _emailService = emailService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task Consume(ConsumeContext<OrderShippedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing OrderShippedEvent for OrderId: {OrderId}, Tracking: {TrackingNumber}",
                message.OrderId, message.TrackingNumber);

            await _unitOfWork.BeginTransactionAsync(context.CancellationToken);

            try
            {
                var order = await _orderRepository.GetByIdAsync(message.OrderId, context.CancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", message.OrderId);
                    return;
                }

                if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Processing)
                {
                    _logger.LogWarning("Order {OrderId} cannot be shipped. Current status: {Status}",
                        message.OrderId, order.Status);
                    return;
                }

                order.MarkAsShipped(message.TrackingNumber);
                await _orderRepository.UpdateAsync(order, context.CancellationToken);

                var customer = await _customerRepository.GetByIdAsync(order.CustomerId, context.CancellationToken);
                if (customer != null)
                {
                    await _emailService.SendShippingNotificationAsync(
                        message.OrderId,
                        customer.Email,
                        message.TrackingNumber,
                        context.CancellationToken);
                }

                await CheckInventoryAfterShipping(order, context.CancellationToken);

                await _unitOfWork.CommitTransactionAsync(context.CancellationToken);

                _logger.LogInformation("Order shipped event processed successfully for OrderId: {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(context.CancellationToken);
                _logger.LogError(ex, "Error processing OrderShippedEvent for OrderId: {OrderId}", message.OrderId);
                throw;
            }
        }

        private async Task CheckInventoryAfterShipping(Domain.Entities.Order order, CancellationToken cancellationToken)
        {
            foreach (var item in order.Items)
            {
                _logger.LogDebug("Checking inventory for product: {ProductId} after shipping", item.ProductId);
            }
        }
    }
}
