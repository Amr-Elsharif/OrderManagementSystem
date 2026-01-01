using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Application.Commands.Orders.CreateOrder;
using OrderManagementSystem.Application.Commands.Orders.UpdateOrder;
using OrderManagementSystem.Application.DTOs.Orders;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Queries.Orders;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController(IMediator mediator, ILogger<OrdersController> logger) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<OrdersController> _logger = logger;

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
        {
            try
            {
                var query = new GetOrderByIdQuery { OrderId = id };
                var order = await _mediator.Send(query);
                return Ok(order);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", id);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order: {OrderId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<List<OrderDto>>> GetCustomerOrders(
            Guid customerId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = new GetOrdersByCustomerQuery
                {
                    CustomerId = customerId,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var orders = await _mediator.Send(query);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for customer: {CustomerId}", customerId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateOrder([FromBody] CreateOrderCommandDto dto)
        {
            try
            {
                var command = new CreateOrderCommand
                {
                    CustomerId = dto.CustomerId,
                    Items = [.. dto.Items.Select(i => new CreateOrderItemCommand
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    })],
                    ShippingAddress = dto.ShippingAddress,
                    Notes = dto.Notes,
                    CreatedBy = User.Identity?.Name ?? "system"
                };

                var orderId = await _mediator.Send(command);

                _logger.LogInformation("Order created successfully: {OrderId}", orderId);

                return CreatedAtAction(nameof(GetOrder), new { id = orderId }, new { orderId });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error creating order");
                return BadRequest(new { error = ex.Message });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found creating order");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateOrderStatus(
            Guid id,
            [FromBody] UpdateOrderStatusDto dto)
        {
            try
            {
                var command = new UpdateOrderStatusCommand
                {
                    OrderId = id,
                    Status = dto.Status,
                    Notes = dto.Notes,
                    UpdatedBy = User.Identity?.Name ?? "system"
                };

                var result = await _mediator.Send(command);

                if (result)
                {
                    _logger.LogInformation("Order status updated: {OrderId} to {Status}", id, dto.Status);
                    return NoContent();
                }

                return BadRequest(new { error = "Failed to update order status" });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating order status: {OrderId}", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", id);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: {OrderId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<List<OrderDto>>> GetOrdersByStatus(OrderStatus status)
        {
            try
            {
                var query = new GetOrderByStatusQuery { Status = status };
                var order = await _mediator.Send(query);
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by status: {Status}", status);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}