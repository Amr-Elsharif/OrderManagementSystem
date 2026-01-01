using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Commands.Orders.MarkOrderAsPaid
{
    public class MarkOrderAsPaidCommandHandler(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        ICacheService cacheService,
        ILogger<MarkOrderAsPaidCommandHandler> logger) : IRequestHandler<MarkOrderAsPaidCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMessagePublisher _messagePublisher = messagePublisher;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<MarkOrderAsPaidCommandHandler> _logger = logger;

        public async Task<bool> Handle(MarkOrderAsPaidCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with ID {request.OrderId} not found");

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
            {
                throw new BusinessRuleException($"Cannot mark order as paid with status {order.Status}");
            }

            if (request.AmountPaid != order.TotalAmount.Amount)
            {
                throw new BusinessRuleException(
                    $"Payment amount ({request.AmountPaid:C}) does not match order total ({order.TotalAmount.Amount:C})");
            }

            order.MarkAsPaid(request.PaymentMethod, request.AmountPaid, request.PaidBy);

            if (!string.IsNullOrEmpty(request.PaidBy))
            {
                order.UpdateTimestamps(request.PaidBy);
            }

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await ClearCaches(order, cancellationToken);

            _logger.LogInformation("Order {OrderId} marked as paid: {PaymentMethod}, {AmountPaid:C}",
                order.Id, request.PaymentMethod, request.AmountPaid);

            return true;
        }

        private async Task ClearCaches(Order order, CancellationToken cancellationToken)
        {
            await _cacheService.RemoveAsync($"order_{order.Id}", cancellationToken);
            await _cacheService.RemoveAsync($"customer_orders_{order.CustomerId}", cancellationToken);
        }
    }
}
