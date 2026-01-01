using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Domain.Enums;
using OrderManagementSystem.Infrastructure.Data;

namespace OrderManagementSystem.Infrastructure.Repositories
{
    public class OrderRepository(ApplicationDbContext context) : IOrderRepository
    {
        private readonly DbSet<Order> DbSet = context.Set<Order>();
        private readonly ApplicationDbContext _context = context;

        public async Task<Order> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(o => o.Items)
                .ToListAsync(cancellationToken);
        }

        public async Task<Order> AddAsync(Order entity, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(entity, cancellationToken);
            return entity;
        }

        public Task UpdateAsync(Order entity, CancellationToken cancellationToken = default)
        {
            DbSet.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Order entity, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await DbSet.AnyAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<Order> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(o => o.Items)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(o => o.Items)
                .Where(o => o.Status == status)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetOrdersInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(o => o.Items)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasActiveOrdersForProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.ProductId == productId)
                .Where(oi => oi.Order.Status != OrderStatus.Cancelled &&
                             oi.Order.Status != OrderStatus.Refunded &&
                             oi.Order.Status != OrderStatus.Delivered)
                .AnyAsync(cancellationToken);
        }

        public async Task<int> GetActiveOrderCountForProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.ProductId == productId)
                .Where(oi => oi.Order.Status != OrderStatus.Cancelled &&
                             oi.Order.Status != OrderStatus.Refunded &&
                             oi.Order.Status != OrderStatus.Delivered)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetOrdersContainingProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.ProductId == productId))
                .Where(o => o.Status != OrderStatus.Cancelled &&
                           o.Status != OrderStatus.Refunded &&
                           o.Status != OrderStatus.Delivered)
                .ToListAsync(cancellationToken);
        }
    }
}