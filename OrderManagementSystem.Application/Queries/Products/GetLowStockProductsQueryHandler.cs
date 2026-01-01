using AutoMapper;
using MediatR;
using OrderManagementSystem.Application.DTOs.Products;
using OrderManagementSystem.Application.Interfaces;

namespace OrderManagementSystem.Application.Queries.Products
{
    public class GetLowStockProductsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService) : IRequestHandler<GetLowStockProductsQuery, List<ProductDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ICacheService _cacheService = cacheService;

        public async Task<List<ProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"products_low_stock_{request.Threshold}";
            var cachedProducts = await _cacheService.GetAsync<List<ProductDto>>(cacheKey, cancellationToken);

            if (cachedProducts != null)
            {
                return cachedProducts;
            }

            var lowStockProducts = await _unitOfWork.Products.GetLowStockProductsAsync(cancellationToken);

            if (request.Threshold.HasValue)
            {
                lowStockProducts = [.. lowStockProducts.Where(p => p.StockQuantity <= request.Threshold.Value)];
            }

            var productDtos = _mapper.Map<List<ProductDto>>(lowStockProducts);

            // Cache for 2 minutes (low stock changes frequently)
            await _cacheService.SetAsync(cacheKey, productDtos, TimeSpan.FromMinutes(2), cancellationToken);

            return productDtos;
        }
    }
}
