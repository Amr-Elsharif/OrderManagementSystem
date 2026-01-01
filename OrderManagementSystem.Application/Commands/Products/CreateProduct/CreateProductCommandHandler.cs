using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Domain.Events.Products;

namespace OrderManagementSystem.Application.Commands.Products.CreateProduct
{
    public class CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        ILogger<CreateProductCommandHandler> logger,
        ICacheService cacheService) : IRequestHandler<CreateProductCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMessagePublisher _messagePublisher = messagePublisher;
        private readonly ILogger<CreateProductCommandHandler> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingProduct = await _unitOfWork.Products.GetBySkuAsync(request.Sku, cancellationToken);
                if (existingProduct != null)
                {
                    throw new ValidationException($"Product with SKU '{request.Sku}' already exists");
                }

                var product = new Product(
                    request.Name,
                    request.Description,
                    request.Sku,
                    request.Price,
                    request.StockQuantity,
                    request.Category,
                    request.CreatedBy);

                await _unitOfWork.Products.AddAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await ClearProductCaches(cancellationToken);

                foreach (var domainEvent in product.DomainEvents)
                {
                    if (domainEvent is ProductLowStockEvent lowStockEvent)
                    {
                        await _messagePublisher.PublishAsync(lowStockEvent, cancellationToken);
                    }
                }

                _logger.LogInformation("Product created: {ProductId}, SKU: {Sku}", product.Id, product.Sku);

                return product.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product with SKU: {Sku}", request.Sku);
                throw;
            }
        }

        private async Task ClearProductCaches(CancellationToken cancellationToken)
        {
            var cacheKeys = new[]
            {
                "products_all_active",
                "products_category_*",
                "products_low_stock"
            };

            foreach (var pattern in cacheKeys)
            {
                await _cacheService.RemoveAsync(pattern, cancellationToken);
            }
        }
    }
}
