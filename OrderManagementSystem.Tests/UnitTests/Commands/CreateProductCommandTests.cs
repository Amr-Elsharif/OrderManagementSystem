using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using Moq;
using OrderManagementSystem.Application.Commands.Products.CreateProduct;
using OrderManagementSystem.Application.Exceptions;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Application.Validators.Products;

namespace OrderManagementSystem.Tests.Application.Commands
{
    public class CreateProductCommandTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessagePublisher> _mockMessagePublisher;
        private readonly Mock<ILogger<CreateProductCommandHandler>> _mockLogger;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CreateProductCommandHandler _handler;
        private readonly CreateProductCommandValidator _validator;

        public CreateProductCommandTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessagePublisher = new Mock<IMessagePublisher>();
            _mockLogger = new Mock<ILogger<CreateProductCommandHandler>>();
            _mockCacheService = new Mock<ICacheService>();
            _handler = new CreateProductCommandHandler(
                _mockUnitOfWork.Object,
                _mockMessagePublisher.Object,
                _mockLogger.Object,
                _mockCacheService.Object);
            _validator = new CreateProductCommandValidator();
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsProductId()
        {
            // Arrange
            var command = new CreateProductCommand
            {
                Name = "Test Product",
                Description = "Test Description",
                Sku = "TEST-SKU-001",
                Price = 99.99m,
                StockQuantity = 100,
                Category = "Electronics",
                CreatedBy = "test_user"
            };

            _mockUnitOfWork.Setup(u => u.Products.GetBySkuAsync(command.Sku, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Product)null);

            _mockUnitOfWork.Setup(u => u.Products.AddAsync(It.IsAny<Domain.Entities.Product>(), It.IsAny<CancellationToken>()))
                .Returns((Task<Domain.Entities.Product>)Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _mockUnitOfWork.Verify(u => u.Products.AddAsync(It.IsAny<Domain.Entities.Product>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DuplicateSku_ThrowsValidationException()
        {
            // Arrange
            var command = new CreateProductCommand
            {
                Name = "Test Product",
                Description = "Test Description",
                Sku = "EXISTING-SKU",
                Price = 99.99m,
                StockQuantity = 100,
                Category = "Electronics"
            };

            var existingProduct = new Domain.Entities.Product(
                "Existing Product",
                "Description",
                "EXISTING-SKU",
                50,
                10,
                "Category");

            _mockUnitOfWork.Setup(u => u.Products.GetBySkuAsync(command.Sku, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingProduct);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public void Validator_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new CreateProductCommand
            {
                Name = "Test Product",
                Description = "Test Description",
                Sku = "TEST-SKU-001",
                Price = 99.99m,
                StockQuantity = 100,
                Category = "Electronics"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validator_InvalidSku_ShouldFail()
        {
            // Arrange
            var command = new CreateProductCommand
            {
                Name = "Test Product",
                Description = "Test Description",
                Sku = "invalid sku", // Lowercase and space
                Price = 99.99m,
                StockQuantity = 100,
                Category = "Electronics"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Sku);
        }

        [Fact]
        public void Validator_InvalidPrice_ShouldFail()
        {
            // Arrange
            var command = new CreateProductCommand
            {
                Name = "Test Product",
                Description = "Test Description",
                Sku = "TEST-SKU-001",
                Price = -10, // Negative price
                StockQuantity = 100,
                Category = "Electronics"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Price);
        }
    }
}