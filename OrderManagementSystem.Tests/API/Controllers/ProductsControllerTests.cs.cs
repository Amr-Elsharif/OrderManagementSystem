using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderManagementSystem.API.Controllers;
using OrderManagementSystem.Application.Commands.Products.CreateProduct;
using OrderManagementSystem.Application.DTOs.Products;
using OrderManagementSystem.Application.Queries.Products;

namespace OrderManagementSystem.Tests.API.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ILogger<ProductsController>> _mockLogger;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<ProductsController>>();
            _controller = new ProductsController(_mockMediator.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetProduct_ValidId_ReturnsOk()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var expectedProduct = new ProductDto
            {
                Id = productId,
                Name = "Test Product",
                Sku = "TEST-001",
                Price = 99.99m
            };

            _mockMediator.Setup(m => m.Send(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedProduct);

            // Act
            var result = await _controller.GetProduct(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedProduct = Assert.IsType<ProductDto>(okResult.Value);
            Assert.Equal(productId, returnedProduct.Id);
        }

        [Fact]
        public async Task CreateProduct_ValidRequest_ReturnsCreated()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Name = "New Product",
                Description = "Description",
                Sku = "NEW-SKU-001",
                Price = 49.99m,
                StockQuantity = 50,
                Category = "Category"
            };

            var expectedProductId = Guid.NewGuid();

            _mockMediator.Setup(m => m.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedProductId);

            // Act
            var result = await _controller.CreateProduct(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(ProductsController.GetProduct), createdResult.ActionName);
            Assert.Equal(expectedProductId, ((dynamic)createdResult.Value).productId);
        }
    }
}