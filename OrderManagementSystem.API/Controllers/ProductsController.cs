using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Application.Commands.Products.CreateProduct;
using OrderManagementSystem.Application.Commands.Products.DeleteProduct;
using OrderManagementSystem.Application.Commands.Products.UpdateProduct;
using OrderManagementSystem.Application.Commands.Products.UpdateProductStock;
using OrderManagementSystem.Application.DTOs.Common;
using OrderManagementSystem.Application.DTOs.Products;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Queries.Products;

namespace OrderManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(IMediator mediator, ILogger<ProductsController> logger) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<ProductsController> _logger = logger;

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? category = null,
            [FromQuery] bool? isActive = true,
            [FromQuery] string? searchTerm = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] bool sortDescending = false)
        {
            try
            {
                var query = new GetProductsQuery
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Category = category,
                    IsActive = isActive,
                    SearchTerm = searchTerm,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET: api/products/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
        {
            try
            {
                var query = new GetProductByIdQuery { ProductId = id };
                var product = await _mediator.Send(query);
                return Ok(product);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", id);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product: {ProductId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET: api/products/sku/{sku}
        [HttpGet("sku/{sku}")]
        public async Task<ActionResult<ProductDto>> GetProductBySku(string sku)
        {
            try
            {
                var query = new GetProductBySkuQuery { Sku = sku };
                var product = await _mediator.Send(query);
                return Ok(product);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found with SKU: {Sku}", sku);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by SKU: {Sku}", sku);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET: api/products/low-stock
        [HttpGet("low-stock")]
        public async Task<ActionResult<List<ProductDto>>> GetLowStockProducts([FromQuery] int? threshold = null)
        {
            try
            {
                var query = new GetLowStockProductsQuery { Threshold = threshold };
                var products = await _mediator.Send(query);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock products");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Guid>> CreateProduct([FromBody] CreateProductDto dto)
        {
            try
            {
                var command = new CreateProductCommand
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Sku = dto.Sku,
                    Price = dto.Price,
                    StockQuantity = dto.StockQuantity,
                    Category = dto.Category,
                    CreatedBy = User.Identity?.Name ?? "system"
                };

                var productId = await _mediator.Send(command);

                _logger.LogInformation("Product created: {ProductId}", productId);

                return CreatedAtAction(nameof(GetProduct), new { id = productId }, new { productId });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error creating product");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT: api/products/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                var command = new UpdateProductCommand
                {
                    ProductId = id,
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    Category = dto.Category,
                    UpdatedBy = User.Identity?.Name ?? "system"
                };

                var result = await _mediator.Send(command);

                if (result)
                {
                    _logger.LogInformation("Product updated: {ProductId}", id);
                    return NoContent();
                }

                return BadRequest(new { error = "Failed to update product" });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", id);
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating product: {ProductId}", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PATCH: api/products/{id}/stock
        [HttpPatch("{id:guid}/stock")]
        public async Task<ActionResult> UpdateProductStock(Guid id, [FromBody] UpdateProductStockDto dto)
        {
            try
            {
                var command = new UpdateProductStockCommand
                {
                    ProductId = id,
                    QuantityChange = dto.QuantityChange,
                    Reason = dto.Reason,
                    UpdatedBy = User.Identity?.Name ?? "system"
                };

                var result = await _mediator.Send(command);

                if (result)
                {
                    _logger.LogInformation("Product stock updated: {ProductId}", id);
                    return NoContent();
                }

                return BadRequest(new { error = "Failed to update product stock" });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", id);
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating product stock: {ProductId}", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product stock: {ProductId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteProduct(Guid id, [FromQuery] bool softDelete = true)
        {
            try
            {
                var command = new DeleteProductCommand
                {
                    ProductId = id,
                    SoftDelete = softDelete,
                    DeletedBy = User.Identity?.Name ?? "system"
                };

                var result = await _mediator.Send(command);

                if (result)
                {
                    _logger.LogInformation("Product deleted: {ProductId}", id);
                    return NoContent();
                }

                return BadRequest(new { error = "Failed to delete product" });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: {ProductId}", id);
                return NotFound(new { error = ex.Message });
            }
            catch (BusinessRuleException ex)
            {
                _logger.LogWarning(ex, "Business rule violation deleting product: {ProductId}", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST: api/products/{id}/activate
        [HttpPost("{id:guid}/activate")]
        public async Task<ActionResult> ActivateProduct(Guid id)
        {
            try
            {
                var query = new GetProductByIdQuery { ProductId = id };
                var productDto = await _mediator.Send(query);

                var updateCommand = new UpdateProductCommand
                {
                    ProductId = id,
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    Category = productDto.Category,
                    UpdatedBy = User.Identity?.Name ?? "system"
                };

                var result = await _mediator.Send(updateCommand);

                if (result)
                {
                    _logger.LogInformation("Product activated: {ProductId}", id);
                    return NoContent();
                }

                return BadRequest(new { error = "Failed to activate product" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating product: {ProductId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}