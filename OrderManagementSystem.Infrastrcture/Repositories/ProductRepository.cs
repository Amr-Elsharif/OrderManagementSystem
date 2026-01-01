using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Infrastructure.Data;

namespace OrderManagementSystem.Infrastructure.Repositories
{
    public class ProductRepository(ApplicationDbContext context) : IProductRepository
    {
        private readonly DbSet<Product> DbSet = context.Set<Product>();

        public async Task<Product> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await DbSet.FindAsync([id], cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet.ToListAsync(cancellationToken);
        }

        public async Task<Product> AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(entity, cancellationToken);
            return entity;
        }

        public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
        {
            DbSet.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Product entity, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await DbSet.AnyAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Product> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(p => p.Category == category)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(p => p.StockQuantity <= p.MinStockThreshold)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetAlternativeProductsAsync(
           Guid excludeProductId,
           string category,
           int limit,
           CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(p => p.Id != excludeProductId &&
                           p.Category == category &&
                           p.IsActive &&
                           p.StockQuantity > 0)
                .OrderByDescending(p => p.StockQuantity)
                .ThenBy(p => p.Price)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}
