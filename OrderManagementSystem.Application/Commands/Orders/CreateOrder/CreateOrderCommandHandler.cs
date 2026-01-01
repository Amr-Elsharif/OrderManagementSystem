using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Domain.Events.Orders;

namespace OrderManagementSystem.Application.Commands.Orders.CreateOrder
{
    public class CreateOrderCommandHandler(
            IUnitOfWork unitOfWork,
            IMessagePublisher messagePublisher,
            ILogger<CreateOrderCommandHandler> logger,
            ICacheService cacheService) : IRequestHandler<CreateOrderCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMessagePublisher _messagePublisher = messagePublisher;
        private readonly ILogger<CreateOrderCommandHandler> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. Validate customer exists
                var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);
                if (customer == null)
                {
                    throw new NotFoundException($"Customer with ID {request.CustomerId} not found");
                }

                // 2. Create order
                var order = new Order(request.CustomerId, request.CreatedBy);

                if (!string.IsNullOrEmpty(request.ShippingAddress))
                {
                    order.UpdateShippingAddress(request.ShippingAddress);
                }

                // 3. Add items to order with validation
                foreach (var item in request.Items)
                {
                    var cacheKey = $"product_{item.ProductId}";
                    var product = await _cacheService.GetAsync<Product>(cacheKey, cancellationToken);

                    if (product == null)
                    {
                        product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                        if (product == null)
                        {
                            throw new NotFoundException($"Product with ID {item.ProductId} not found");
                        }

                        await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5), cancellationToken);
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        throw new ValidationException($"Insufficient stock for product {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                    }

                    product.ReduceStock(item.Quantity);
                    await _unitOfWork.Products.UpdateAsync(product, cancellationToken);

                    order.AddItem(product.Id, product.Name, item.Quantity, product.Price);
                }

                // 4. Save order
                await _unitOfWork.Orders.AddAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 5. Publish events
                foreach (var domainEvent in order.DomainEvents)
                {
                    if (domainEvent is OrderCreatedEvent orderCreatedEvent)
                    {
                        await _messagePublisher.PublishAsync(orderCreatedEvent, cancellationToken);
                    }
                }

                // 6. Commit transaction
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // 7. Clear customer orders cache
                await _cacheService.RemoveAsync($"customer_orders_{request.CustomerId}", cancellationToken);

                _logger.LogInformation("Order created successfully: {OrderId}, OrderNumber: {OrderNumber}",
                    order.Id, order.OrderNumber);

                return order.Id;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error creating order for customer {CustomerId}", request.CustomerId);
                throw;
            }
        }
    }
}