using Microsoft.Extensions.Logging;
using Moq;
using OrderManagementSystem.Application.Commands.Orders.CreateOrder;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Application.Validators.Orders;
using OrderManagementSystem.Domain.Entities;

namespace OrderManagementSystem.Tests.UnitTests.Commands
{
    public class CreateOrderCommandTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessagePublisher> _mockMessagePublisher;
        private readonly Mock<ILogger<CreateOrderCommandHandler>> _mockLogger;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CreateOrderCommandHandler _handler;
        private readonly CreateOrderCommandValidator _validator;

        public CreateOrderCommandTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessagePublisher = new Mock<IMessagePublisher>();
            _mockLogger = new Mock<ILogger<CreateOrderCommandHandler>>();
            _mockCacheService = new Mock<ICacheService>();
            _handler = new CreateOrderCommandHandler(
                _mockUnitOfWork.Object,
                _mockMessagePublisher.Object,
                _mockLogger.Object,
                _mockCacheService.Object);
            _validator = new CreateOrderCommandValidator();
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsOrderId()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                Items =
                [
                    new() { ProductId = productId, Quantity = 2 }
                ],
                CreatedBy = "test_user"
            };

            var customer = new Customer("Amr", "Elsharif", "amrelsharif9@gmail.com");
            var product = new Product("Test Product", "Description", "SKU123", 100, 10, "Category");

            _mockUnitOfWork.Setup(u => u.Customers.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _mockUnitOfWork.Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            _mockCacheService.Setup(c => c.GetAsync<Product>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _mockUnitOfWork.Verify(u => u.Orders.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Validator_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items =
                [
                    new() { ProductId = Guid.NewGuid(), Quantity = 2 }
                ]
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validator_InvalidCommand_ShouldFail()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.Empty,
                Items = []
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "CustomerId");
            Assert.Contains(result.Errors, e => e.PropertyName == "Items");
        }
    }
}