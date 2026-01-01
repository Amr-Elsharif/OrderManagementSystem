using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Infrastructure.Messaging.Order
{
    public class OrderCancelledConsumer(
        ILogger<OrderCancelledConsumer> logger,
        IEmailService emailService,
        IUnitOfWork unitOfWork) : IConsumer<OrderCancelledEvent>
    {
        private readonly ILogger<OrderCancelledConsumer> _logger = logger;
        private readonly IEmailService _emailService = emailService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing OrderCancelledEvent for OrderId: {OrderId}, Reason: {Reason}",
                message.OrderId, message.Reason);

            await _unitOfWork.BeginTransactionAsync(context.CancellationToken);

            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(message.OrderId, context.CancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", message.OrderId);
                    return;
                }

                foreach (var orderItem in order.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(orderItem.ProductId, context.CancellationToken);
                    if (product != null)
                    {
                        product.IncreaseStock(orderItem.Quantity);
                        await _unitOfWork.Products.UpdateAsync(product, context.CancellationToken);

                        _logger.LogDebug("Restored {Quantity} units of product {ProductId} to inventory",
                            orderItem.Quantity, orderItem.ProductId);
                    }
                }

                var customer = await _unitOfWork.Customers.GetByIdAsync(order.CustomerId, context.CancellationToken);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found: {CustomerId}", order.CustomerId);
                    return;
                }

                await _emailService.SendCancellationNotificationAsync(
                    message.OrderId,
                    customer.Email,
                    message.Reason,
                    context.CancellationToken);

                await UpdateCancellationAnalytics(order, message.Reason);

                await _unitOfWork.CommitTransactionAsync(context.CancellationToken);

                _logger.LogInformation("Order cancelled event processed successfully for OrderId: {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(context.CancellationToken);
                _logger.LogError(ex, "Error processing OrderCancelledEvent for OrderId: {OrderId}", message.OrderId);
                throw;
            }
        }

        private async Task UpdateCancellationAnalytics(Domain.Entities.Order order, string reason)
        {
            var cancellationStats = new
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                TotalAmount = order.TotalAmount.Amount,
                CancellationReason = reason,
                CancelledAt = DateTime.UtcNow
            };

            _logger.LogDebug("Updated cancellation analytics for OrderId: {OrderId}", order.Id);
        }
    }
}
