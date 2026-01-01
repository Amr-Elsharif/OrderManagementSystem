using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;

namespace OrderManagementSystem.Application.Commands.Products.UpdateProduct
{
    public class UpdateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductCommandHandler> logger,
        ICacheService cacheService) : IRequestHandler<UpdateProductCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<UpdateProductCommandHandler> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null)
            {
                throw new NotFoundException($"Product with ID {request.ProductId} not found");
            }

            product.UpdateDetails(
                request.Name,
                request.Description,
                request.Price,
                request.Category);

            if (!string.IsNullOrEmpty(request.UpdatedBy))
            {
                product.UpdateTimestamps(request.UpdatedBy);
            }

            await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await ClearProductCaches(product.Id, cancellationToken);

            _logger.LogInformation("Product updated: {ProductId}, Name: {Name}", product.Id, product.Name);

            return true;
        }

        private async Task ClearProductCaches(Guid productId, CancellationToken cancellationToken)
        {
            await _cacheService.RemoveAsync($"product_{productId}", cancellationToken);
            await _cacheService.RemoveAsync("products_all_active", cancellationToken);
        }
    }
}
