using MassTransit;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Infrastructure.Messaging.Order
{
    public class OrderPaidConsumer(
        ILogger<OrderPaidConsumer> logger,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ICacheService cacheService) : IConsumer<OrderPaidEvent>
    {
        private readonly ILogger<OrderPaidConsumer> _logger = logger;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IEmailService _emailService = emailService;
        private readonly ICacheService _cacheService = cacheService;

        public async Task Consume(ConsumeContext<OrderPaidEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("Processing OrderPaidEvent for OrderId: {OrderId}, Amount: {AmountPaid}, Method: {PaymentMethod}",
                message.OrderId, message.AmountPaid, message.PaymentMethod);

            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(message.OrderId, context.CancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", message.OrderId);
                    return;
                }

                if (order.TotalAmount.Amount != message.AmountPaid)
                {
                    _logger.LogWarning("Payment amount mismatch for OrderId: {OrderId}. Expected: {Expected}, Received: {Received}",
                        message.OrderId, order.TotalAmount.Amount, message.AmountPaid);
                }

                order.MarkAsPaid(message.PaymentMethod, message.AmountPaid);
                await _unitOfWork.Orders.UpdateAsync(order, context.CancellationToken);

                await _cacheService.RemoveAsync($"order_{message.OrderId}", context.CancellationToken);
                await _cacheService.RemoveAsync($"customer_orders_{order.CustomerId}", context.CancellationToken);

                var customer = await _unitOfWork.Customers.GetByIdAsync(order.CustomerId, context.CancellationToken);

                await _emailService.SendPaymentConfirmationAsync(
                    message.OrderId,
                    customer.Email,
                    message.AmountPaid,
                    message.PaymentMethod,
                    context.CancellationToken);

                await UpdateSalesAnalytics(message, context.CancellationToken);

                _logger.LogInformation("Order paid event processed successfully for OrderId: {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OrderPaidEvent for OrderId: {OrderId}", message.OrderId);
                throw;
            }
        }

        private async Task UpdateSalesAnalytics(OrderPaidEvent paymentEvent, CancellationToken cancellationToken)
        {
            var analyticsKey = $"sales_daily_{DateTime.UtcNow:yyyyMMdd}";
            var dailySales = await _cacheService.GetAsync<decimal>(analyticsKey, cancellationToken);
            dailySales += paymentEvent.AmountPaid;
            await _cacheService.SetAsync(analyticsKey, dailySales, TimeSpan.FromDays(1), cancellationToken);

            _logger.LogDebug("Updated sales analytics for day: {Date}", DateTime.UtcNow.ToString("yyyyMMdd"));
        }
    }
}
