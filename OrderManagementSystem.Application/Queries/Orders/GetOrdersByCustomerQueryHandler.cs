using AutoMapper;
using MediatR;
using OrderManagementSystem.Application.DTOs.Orders;
using OrderManagementSystem.Application.Interfaces;

namespace OrderManagementSystem.Application.Queries.Orders
{
    public class GetOrdersByCustomerQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService) : IRequestHandler<GetOrdersByCustomerQuery, List<OrderDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ICacheService _cacheService = cacheService;

        public async Task<List<OrderDto>> Handle(GetOrdersByCustomerQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"customer_orders_{request.CustomerId}_page_{request.PageNumber}_size_{request.PageSize}";
            var cachedOrders = await _cacheService.GetAsync<List<OrderDto>>(cacheKey, cancellationToken);

            if (cachedOrders != null)
            {
                return cachedOrders;
            }

            var orders = await _unitOfWork.Orders.GetOrdersByCustomerIdAsync(request.CustomerId, cancellationToken);

            var pagedOrders = orders
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var orderDtos = _mapper.Map<List<OrderDto>>(pagedOrders);

            await _cacheService.SetAsync(cacheKey, orderDtos, TimeSpan.FromMinutes(5), cancellationToken);

            return orderDtos;
        }
    }
}
