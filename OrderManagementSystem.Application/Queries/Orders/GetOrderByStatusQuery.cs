using MediatR;
using OrderManagementSystem.Application.DTOs.Orders;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Queries.Orders
{
    public class GetOrderByStatusQuery : IRequest<List<OrderDto>>
    {
        public OrderStatus Status { get; init; }
    }
}