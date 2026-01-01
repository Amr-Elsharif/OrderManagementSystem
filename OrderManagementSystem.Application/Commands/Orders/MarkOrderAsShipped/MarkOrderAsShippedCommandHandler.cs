using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Commands.Orders.MarkOrderAsShipped
{
    public class MarkOrderAsShippedCommandHandler(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        ICacheService cacheService,
        ILogger<MarkOrderAsShippedCommandHandler> logger) : IRequestHandler<MarkOrderAsShippedCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMessagePublisher _messagePublisher = messagePublisher;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<MarkOrderAsShippedCommandHandler> _logger = logger;

        public async Task<bool> Handle(MarkOrderAsShippedCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with ID {request.OrderId} not found");

            if (order.Status != OrderStatus.Paid)
            {
                throw new BusinessRuleException($"Cannot ship order with status {order.Status}. Order must be paid first.");
            }

            order.MarkAsShipped(request.TrackingNumber, request.ShippedBy);

            if (!string.IsNullOrEmpty(request.ShippedBy))
            {
                order.UpdateTimestamps(request.ShippedBy);
            }

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await ClearCaches(order, cancellationToken);

            _logger.LogInformation("Order {OrderId} marked as shipped: Tracking {TrackingNumber}",
                order.Id, request.TrackingNumber);

            return true;
        }

        private async Task ClearCaches(Order order, CancellationToken cancellationToken)
        {
            await _cacheService.RemoveAsync($"order_{order.Id}", cancellationToken);
            await _cacheService.RemoveAsync($"customer_orders_{order.CustomerId}", cancellationToken);
        }
    }
}
