using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Commands.Orders.UpdateOrder
{
    public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

        public UpdateOrderStatusCommandHandler(
            IUnitOfWork unitOfWork,
            IMessagePublisher messagePublisher,
            ILogger<UpdateOrderStatusCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException($"Order with ID {request.OrderId} not found");
            }

            switch (request.Status)
            {
                case OrderStatus.Paid:
                    // In real app, you would have payment info
                    order.MarkAsPaid("CreditCard", order.TotalAmount.Amount);
                    break;

                case OrderStatus.Shipped:
                    order.MarkAsShipped(Guid.NewGuid().ToString()); // Generate tracking number
                    break;

                case OrderStatus.Cancelled:
                    if (string.IsNullOrEmpty(request.Notes))
                        throw new ValidationException("Cancellation reason is required");
                    order.CancelOrder(request.Notes);
                    break;

                default:
                    throw new ValidationException($"Invalid status transition to {request.Status}");
            }

            if (!string.IsNullOrEmpty(request.UpdatedBy))
            {
                order.UpdateTimestamps(request.UpdatedBy);
            }

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Publish domain events
            foreach (var domainEvent in order.DomainEvents)
            {
                await _messagePublisher.PublishAsync(domainEvent, cancellationToken);
            }

            _logger.LogInformation("Order {OrderId} status updated to {Status}",
                order.Id, order.Status);

            return true;
        }
    }
}
