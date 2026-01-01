using AutoMapper;
using MediatR;
using OrderManagementSystem.Application.DTOs.Products;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;

namespace OrderManagementSystem.Application.Queries.Products
{
    public class GetProductBySkuQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService) : IRequestHandler<GetProductBySkuQuery, ProductDto>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ICacheService _cacheService = cacheService;

        public async Task<ProductDto> Handle(GetProductBySkuQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"product_sku_{request.Sku.ToUpperInvariant()}";
            var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);

            if (cachedProduct != null)
            {
                return cachedProduct;
            }

            var product = await _unitOfWork.Products.GetBySkuAsync(request.Sku, cancellationToken) ?? throw new NotFoundException($"Product with SKU '{request.Sku}' not found");
            var productDto = _mapper.Map<ProductDto>(product);

            await _cacheService.SetAsync(cacheKey, productDto, TimeSpan.FromMinutes(10), cancellationToken);

            return productDto;
        }
    }
}
