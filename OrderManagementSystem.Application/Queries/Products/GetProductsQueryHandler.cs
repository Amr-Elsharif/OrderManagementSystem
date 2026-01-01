using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.DTOs.Common;
using OrderManagementSystem.Application.DTOs.Products;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Entities;

namespace OrderManagementSystem.Application.Queries.Products
{
    public class GetProductsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetProductsQueryHandler> logger) : IRequestHandler<GetProductsQuery, PaginatedResult<ProductDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<GetProductsQueryHandler> _logger = logger;

        public async Task<PaginatedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = GenerateCacheKey(request);
            var cachedResult = await _cacheService.GetAsync<PaginatedResult<ProductDto>>(cacheKey, cancellationToken);

            if (cachedResult != null)
            {
                _logger.LogDebug("Products query result retrieved from cache: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var allProducts = await _unitOfWork.Products.GetAllAsync(cancellationToken);

            var filteredProducts = ApplyFilters(allProducts, request);

            var sortedProducts = ApplySorting(filteredProducts, request);

            var totalCount = sortedProducts.Count();
            var pagedProducts = sortedProducts
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var productDtos = _mapper.Map<List<ProductDto>>(pagedProducts);

            var result = new PaginatedResult<ProductDto>(
                productDtos,
                request.PageNumber,
                request.PageSize,
                totalCount);

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

            _logger.LogDebug("Products query executed: {ProductCount} products found", totalCount);

            return result;
        }

        private static string GenerateCacheKey(GetProductsQuery query)
        {
            return $"products_page_{query.PageNumber}_size_{query.PageSize}" +
                   $"_{query.Category}_{query.IsActive}_{query.SearchTerm}" +
                   $"_{query.MinPrice}_{query.MaxPrice}_{query.SortBy}_{query.SortDescending}";
        }

        private static IEnumerable<Product> ApplyFilters(IEnumerable<Product> products, GetProductsQuery query)
        {
            var filtered = products.AsEnumerable();

            if (!string.IsNullOrEmpty(query.Category))
            {
                filtered = filtered.Where(p => p.Category.Equals(query.Category, StringComparison.OrdinalIgnoreCase));
            }

            if (query.IsActive.HasValue)
            {
                filtered = filtered.Where(p => p.IsActive == query.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                filtered = filtered.Where(p =>
                    p.Name.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Sku.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (query.MinPrice.HasValue)
            {
                filtered = filtered.Where(p => p.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                filtered = filtered.Where(p => p.Price <= query.MaxPrice.Value);
            }

            return filtered;
        }

        private static IEnumerable<Product> ApplySorting(IEnumerable<Product> products, GetProductsQuery query)
        {
            return query.SortBy?.ToLowerInvariant() switch
            {
                "name" => query.SortDescending
                    ? products.OrderByDescending(p => p.Name)
                    : products.OrderBy(p => p.Name),
                "price" => query.SortDescending
                    ? products.OrderByDescending(p => p.Price)
                    : products.OrderBy(p => p.Price),
                "category" => query.SortDescending
                    ? products.OrderByDescending(p => p.Category)
                    : products.OrderBy(p => p.Category),
                _ => query.SortDescending
                    ? products.OrderByDescending(p => p.Name)
                    : products.OrderBy(p => p.Name)
            };
        }
    }
}
