using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<Order> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
        Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<Order>> GetOrdersInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<bool> HasActiveOrdersForProductAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<int> GetActiveOrderCountForProductAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Order>> GetOrdersContainingProductAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
