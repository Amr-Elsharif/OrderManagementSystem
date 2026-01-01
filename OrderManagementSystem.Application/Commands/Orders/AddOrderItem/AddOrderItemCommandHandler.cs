using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Enums;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Application.Commands.Orders.AddOrderItem
{
    public class AddOrderItemCommandHandler(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        ICacheService cacheService,
        ILogger<AddOrderItemCommandHandler> logger) : IRequestHandler<AddOrderItemCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMessagePublisher _messagePublisher = messagePublisher;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<AddOrderItemCommandHandler> _logger = logger;

        public async Task<bool> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
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
                    throw new BusinessRuleException($"Cannot add items to order with status {order.Status}");
                }

                var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
                if (product == null)
                {
                    throw new NotFoundException($"Product with ID {request.ProductId} not found");
                }

                if (!product.IsActive)
                {
                    throw new BusinessRuleException($"Product {product.Name} is not active");
                }

                // Check stock
                if (product.StockQuantity < request.Quantity)
                {
                    throw new BusinessRuleException(
                        $"Insufficient stock for product {product.Name}. " +
                        $"Available: {product.StockQuantity}, Requested: {request.Quantity}");
                }

                product.ReduceStock(request.Quantity);
                await _unitOfWork.Products.UpdateAsync(product, cancellationToken);

                order.AddItem(product.Id, product.Name, request.Quantity, product.Price);

                // Get the added item for event publishing
                var addedItem = order.Items.Last();

                if (!string.IsNullOrEmpty(request.AddedBy))
                {
                    order.UpdateTimestamps(request.AddedBy);
                }

                await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish domain event
                await _messagePublisher.PublishAsync(new OrderItemAddedEvent(
                    order.Id, addedItem.Id, product.Id, request.Quantity, product.Price, request.AddedBy ?? "system"),
                    cancellationToken);

                await ClearCaches(order.Id, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Item added to order {OrderId}: Product {ProductId}, Quantity {Quantity}",
                    order.Id, product.Id, request.Quantity);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error adding item to order {OrderId}", request.OrderId);
                throw;
            }
        }

        private async Task ClearCaches(Guid orderId, CancellationToken cancellationToken)
        {
            await _cacheService.RemoveAsync($"order_{orderId}", cancellationToken);
        }
    }
}
