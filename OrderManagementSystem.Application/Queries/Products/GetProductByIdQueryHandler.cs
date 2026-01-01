using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.DTOs.Products;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;

namespace OrderManagementSystem.Application.Queries.Products
{
    public class GetProductByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetProductByIdQueryHandler> logger) : IRequestHandler<GetProductByIdQuery, ProductDto>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<GetProductByIdQueryHandler> _logger = logger;

        public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"product_{request.ProductId}";
            var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);

            if (cachedProduct != null)
            {
                _logger.LogDebug("Product {ProductId} retrieved from cache", request.ProductId);
                return cachedProduct;
            }

            var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken) ?? throw new NotFoundException($"Product with ID {request.ProductId} not found");

            var productDto = _mapper.Map<ProductDto>(product);

            await _cacheService.SetAsync(cacheKey, productDto, TimeSpan.FromMinutes(10), cancellationToken);

            _logger.LogDebug("Product {ProductId} retrieved from database", request.ProductId);

            return productDto;
        }
    }
}
