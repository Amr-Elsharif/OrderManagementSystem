using OrderManagementSystem.Domain.Entities;

namespace OrderManagementSystem.Application.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Product> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetAlternativeProductsAsync(Guid excludeProductId, string category, int limit, CancellationToken cancellationToken = default);
    }
}
