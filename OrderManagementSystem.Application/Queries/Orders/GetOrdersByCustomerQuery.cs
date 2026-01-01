using MediatR;
using OrderManagementSystem.Application.DTOs.Orders;

namespace OrderManagementSystem.Application.Queries.Orders
{
    public class GetOrdersByCustomerQuery : IRequest<List<OrderDto>>
    {
        public Guid CustomerId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
