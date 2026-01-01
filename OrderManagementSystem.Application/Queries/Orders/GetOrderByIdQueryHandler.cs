using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.DTOs.Orders;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;

namespace OrderManagementSystem.Application.Queries.Orders
{
    public class GetOrderByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetOrderByIdQueryHandler> logger) : IRequestHandler<GetOrderByIdQuery, OrderDto>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<GetOrderByIdQueryHandler> _logger = logger;

        public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"order_{request.OrderId}";
            var cachedOrder = await _cacheService.GetAsync<OrderDto>(cacheKey, cancellationToken);

            if (cachedOrder != null)
            {
                _logger.LogDebug("Order {OrderId} retrieved from cache", request.OrderId);
                return cachedOrder;
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException($"Order with ID {request.OrderId} not found");
            }

            var orderDto = _mapper.Map<OrderDto>(order);

            await _cacheService.SetAsync(cacheKey, orderDto, TimeSpan.FromMinutes(10), cancellationToken);

            _logger.LogDebug("Order {OrderId} retrieved from database and cached", request.OrderId);

            return orderDto;
        }
    }
}
