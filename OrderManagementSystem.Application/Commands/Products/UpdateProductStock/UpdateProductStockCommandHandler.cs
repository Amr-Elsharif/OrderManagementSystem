using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Events.Products;

namespace OrderManagementSystem.Application.Commands.Products.UpdateProductStock
{
    public class UpdateProductStockCommandHandler(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        ILogger<UpdateProductStockCommandHandler> logger,
        ICacheService cacheService) : IRequestHandler<UpdateProductStockCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMessagePublisher _messagePublisher = messagePublisher;
        private readonly ILogger<UpdateProductStockCommandHandler> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task<bool> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken) ?? throw new NotFoundException($"Product with ID {request.ProductId} not found");

            if (request.QuantityChange > 0)
            {
                product.IncreaseStock(request.QuantityChange);
            }
            else if (request.QuantityChange < 0)
            {
                product.ReduceStock(Math.Abs(request.QuantityChange));
            }

            if (!string.IsNullOrEmpty(request.UpdatedBy))
            {
                product.UpdateTimestamps(request.UpdatedBy);
            }

            await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await ClearProductCaches(product.Id, cancellationToken);

            foreach (var domainEvent in product.DomainEvents)
            {
                if (domainEvent is ProductLowStockEvent lowStockEvent)
                {
                    await _messagePublisher.PublishAsync(lowStockEvent, cancellationToken);
                }
            }

            _logger.LogInformation("Product stock updated: {ProductId}, Change: {QuantityChange}, New Stock: {Stock}",
                product.Id, request.QuantityChange, product.StockQuantity);

            return true;
        }

        private async Task ClearProductCaches(Guid productId, CancellationToken cancellationToken)
        {
            await _cacheService.RemoveAsync($"product_{productId}", cancellationToken);
            await _cacheService.RemoveAsync("products_low_stock", cancellationToken);
        }
    }
}
