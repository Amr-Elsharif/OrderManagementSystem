using MediatR;
using OrderManagementSystem.Application.DTOs.Orders;

namespace OrderManagementSystem.Application.Queries.Orders
{
    public class GetOrderByIdQuery : IRequest<OrderDto>
    {
        public Guid OrderId { get; init; }
    }
}