using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.DTOs.Orders;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;

namespace OrderManagementSystem.Application.Queries.Orders
{
    public class GetOrderByStatusQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetOrderByStatusQuery> logger) : IRequestHandler<GetOrderByStatusQuery, List<OrderDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<GetOrderByStatusQuery> _logger = logger;

        public async Task<List<OrderDto>> Handle(GetOrderByStatusQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"order_{request.Status}";
            var cachedOrder = await _cacheService.GetAsync<List<OrderDto>>(cacheKey, cancellationToken);

            if (cachedOrder != null)
            {
                _logger.LogDebug("Order status {Status} retrieved from cache", request.Status);
                return cachedOrder;
            }

            var orders = await _unitOfWork.Orders.GetOrdersByStatusAsync(request.Status, cancellationToken) ?? throw new NotFoundException($"Order with ID {request.Status} not found");

            var orderDto = _mapper.Map<List<OrderDto>>(orders);

            await _cacheService.SetAsync(cacheKey, orderDto, TimeSpan.FromMinutes(10), cancellationToken);

            _logger.LogDebug("Order status {Status} retrieved from database and cached", request.Status);

            return orderDto;
        }
    }
}
