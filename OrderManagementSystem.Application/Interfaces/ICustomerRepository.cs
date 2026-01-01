using OrderManagementSystem.Domain.Entities;

namespace OrderManagementSystem.Application.Interfaces
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default);
    }
}
