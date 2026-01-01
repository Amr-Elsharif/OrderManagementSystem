using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Commands.Orders.RemoveOrderItem
{
    public class RemoveOrderItemCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<RemoveOrderItemCommandHandler> logger) : IRequestHandler<RemoveOrderItemCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<RemoveOrderItemCommandHandler> _logger = logger;

        public async Task<bool> Handle(RemoveOrderItemCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null)
                {
                    throw new NotFoundException($"Order with ID {request.OrderId} not found");
                }

                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
                {
                    throw new BusinessRuleException($"Cannot remove items from order with status {order.Status}");
                }

                var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId)
                    ?? throw new NotFoundException($"Order item with ID {request.OrderItemId} not found in order {request.OrderId}");

                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                if (product != null)
                {
                    product.IncreaseStock(item.Quantity);
                    await _unitOfWork.Products.UpdateAsync(product, cancellationToken);

                    _logger.LogDebug("Restored {Quantity} units of product {ProductId} to inventory",
                        item.Quantity, item.ProductId);
                }

                order.RemoveItem(request.OrderItemId);

                if (!string.IsNullOrEmpty(request.RemovedBy))
                {
                    order.UpdateTimestamps(request.RemovedBy);
                }

                await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await ClearCaches(order.Id, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Item {OrderItemId} removed from order {OrderId}: Product {ProductId}, Quantity {Quantity}",
                    item.Id, order.Id, item.ProductId, item.Quantity);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error removing item from order {OrderId}", request.OrderId);
                throw;
            }
        }

        private async Task ClearCaches(Guid orderId, CancellationToken cancellationToken)
        {
            await _cacheService.RemoveAsync($"order_{orderId}", cancellationToken);
        }
    }
}
