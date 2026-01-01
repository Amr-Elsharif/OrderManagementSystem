using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Commands.Orders.UpdateOrderItemQuantity
{
    public class UpdateOrderItemQuantityCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<UpdateOrderItemQuantityCommandHandler> logger) : IRequestHandler<UpdateOrderItemQuantityCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<UpdateOrderItemQuantityCommandHandler> _logger = logger;

        public async Task<bool> Handle(UpdateOrderItemQuantityCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken) ?? throw new NotFoundException($"Order with ID {request.OrderId} not found");
                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
                {
                    throw new BusinessRuleException($"Cannot update items in order with status {order.Status}");
                }

                var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
                if (item == null)
                {
                    throw new NotFoundException($"Order item with ID {request.OrderItemId} not found in order {request.OrderId}");
                }

                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                if (product == null)
                {
                    throw new NotFoundException($"Product with ID {item.ProductId} not found");
                }

                var quantityDifference = request.NewQuantity - item.Quantity;

                if (quantityDifference > 0)
                {
                    // Increasing quantity - check stock
                    if (product.StockQuantity < quantityDifference)
                    {
                        throw new BusinessRuleException(
                            $"Insufficient stock to increase quantity. " +
                            $"Available: {product.StockQuantity}, Needed: {quantityDifference}");
                    }

                    product.ReduceStock(quantityDifference);
                }
                else if (quantityDifference < 0)
                {
                    product.IncreaseStock(Math.Abs(quantityDifference));
                }
                else
                {
                    // No change
                    return true;
                }

                await _unitOfWork.Products.UpdateAsync(product, cancellationToken);

                order.UpdateQuantity(request.OrderItemId, request.NewQuantity);

                if (!string.IsNullOrEmpty(request.UpdatedBy))
                {
                    order.UpdateTimestamps(request.UpdatedBy);
                }

                await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await ClearCaches(order.Id, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Item {OrderItemId} quantity updated in order {OrderId}: {OldQuantity} → {NewQuantity}",
                    item.Id, order.Id, item.Quantity, request.NewQuantity);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error updating item quantity in order {OrderId}", request.OrderId);
                throw;
            }
        }

        private async Task ClearCaches(Guid orderId, CancellationToken cancellationToken)
        {
            await _cacheService.RemoveAsync($"order_{orderId}", cancellationToken);
        }
    }
}
