using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Commands.Orders.CancelOrder
{
    public class CancelOrderCommandHandler(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        ICacheService cacheService,
        ILogger<CancelOrderCommandHandler> logger) : IRequestHandler<CancelOrderCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMessagePublisher _messagePublisher = messagePublisher;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<CancelOrderCommandHandler> _logger = logger;

        public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken)
                    ?? throw new NotFoundException($"Order with ID {request.OrderId} not found");

                if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
                {
                    throw new BusinessRuleException($"Cannot cancel order with status {order.Status}");
                }

                // Restore stock for all items
                foreach (var item in order.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                    if (product != null)
                    {
                        product.IncreaseStock(item.Quantity);
                        await _unitOfWork.Products.UpdateAsync(product, cancellationToken);

                        _logger.LogDebug("Restored {Quantity} units of product {ProductId} to inventory",
                            item.Quantity, item.ProductId);
                    }
                }

                order.CancelOrder(request.Reason, request.CancelledBy);

                if (!string.IsNullOrEmpty(request.CancelledBy))
                {
                    order.UpdateTimestamps(request.CancelledBy);
                }

                await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await ClearCaches(order, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Order {OrderId} cancelled: {Reason}", order.Id, request.Reason);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error cancelling order {OrderId}", request.OrderId);
                throw;
            }
        }

        private async Task ClearCaches(Order order, CancellationToken cancellationToken)
        {
            await _cacheService.RemoveAsync($"order_{order.Id}", cancellationToken);
            await _cacheService.RemoveAsync($"customer_orders_{order.CustomerId}", cancellationToken);
        }
    }
}
