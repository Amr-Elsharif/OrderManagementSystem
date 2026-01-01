using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using OrderManagementSystem.Application.DTOs.Orders;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Domain.Enums;
using OrderManagementSystem.Infrastructure.Data;
using System.Net.Http.Json;

namespace OrderManagementSystem.Tests.IntegrationTests
{
    public class OrderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ApplicationDbContext _dbContext;

        public OrderIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database with in-memory for testing
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("OrderManagementTestDb");
                    });
                });
            });

            _client = _factory.CreateClient();

            var scope = _factory.Services.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created and seeded
            _dbContext.Database.EnsureCreated();
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Clear existing data
            _dbContext.Orders.RemoveRange(_dbContext.Orders);
            _dbContext.Products.RemoveRange(_dbContext.Products);
            _dbContext.Customers.RemoveRange(_dbContext.Customers);
            _dbContext.SaveChanges();

            // Seed test data
            var customer = new Customer("John", "Doe", "john.doe@test.com", "1234567890", "123 Test St");
            _dbContext.Customers.Add(customer);

            var product1 = new Product("Test Product 1", "Description 1", "SKU-001", 99.99m, 100, "Electronics");
            var product2 = new Product("Test Product 2", "Description 2", "SKU-002", 49.99m, 50, "Electronics");
            _dbContext.Products.AddRange(product1, product2);

            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task CreateOrder_ValidRequest_ReturnsCreatedOrder()
        {
            // Arrange
            var customer = await _dbContext.Customers.FirstAsync();
            var product = await _dbContext.Products.FirstAsync();

            var orderData = new
            {
                customerId = customer.Id,
                items = new[]
                {
                    new
                    {
                        productId = product.Id,
                        quantity = 2
                    }
                },
                shippingAddress = "123 Test Street",
                notes = "Test order"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders", orderData);

            // Assert
            response.EnsureSuccessStatusCode();
            var orderId = await response.Content.ReadFromJsonAsync<Guid>();

            Assert.NotEqual(Guid.Empty, orderId);

            // Verify order was created in database
            var order = await _dbContext.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            Assert.NotNull(order);
            Assert.Single(order.Items);
            Assert.Equal(2, order.Items.First().Quantity);
        }

        [Fact]
        public async Task CreateOrder_InsufficientStock_ReturnsError()
        {
            // Arrange
            var customer = await _dbContext.Customers.FirstAsync();
            var product = await _dbContext.Products.FirstAsync();

            // Update product to have low stock
            product.ReduceStock(95); // Leave only 5 in stock
            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync();

            var orderData = new
            {
                customerId = customer.Id,
                items = new[]
                {
                    new
                    {
                        productId = product.Id,
                        quantity = 10 // Request more than available
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders", orderData);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

            var error = await response.Content.ReadAsStringAsync();
            Assert.Contains("Insufficient stock", error);
        }

        [Fact]
        public async Task GetOrder_ValidId_ReturnsOrder()
        {
            // Arrange
            var customer = await _dbContext.Customers.FirstAsync();
            var product = await _dbContext.Products.FirstAsync();

            var order = new Order(customer.Id);
            order.AddItem(product.Id, product.Name, 1, product.Price);

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync($"/api/orders/{order.Id}");

            // Assert
            response.EnsureSuccessStatusCode();

            var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>();
            Assert.NotNull(orderDto);
            Assert.Equal(order.Id, orderDto.Id);
            Assert.Equal(order.OrderNumber, orderDto.OrderNumber);
        }

        [Fact]
        public async Task UpdateOrderStatus_ValidTransition_UpdatesSuccessfully()
        {
            // Arrange
            var customer = await _dbContext.Customers.FirstAsync();
            var product = await _dbContext.Products.FirstAsync();

            var order = new Order(customer.Id);
            order.AddItem(product.Id, product.Name, 1, product.Price);

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            var updateData = new
            {
                status = "Paid",
                notes = "Payment received"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/orders/{order.Id}/status", updateData);

            // Assert
            response.EnsureSuccessStatusCode();

            // Verify status was updated
            var updatedOrder = await _dbContext.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Paid, updatedOrder.Status);
        }

        [Fact]
        public async Task CancelOrder_WithReason_CancelsSuccessfully()
        {
            // Arrange
            var customer = await _dbContext.Customers.FirstAsync();
            var product = await _dbContext.Products.FirstAsync();

            var order = new Order(customer.Id);
            order.AddItem(product.Id, product.Name, 1, product.Price);

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            var cancelData = new
            {
                status = "Cancelled",
                notes = "Customer requested cancellation due to change of mind"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/orders/{order.Id}/status", cancelData);

            // Assert
            response.EnsureSuccessStatusCode();

            var cancelledOrder = await _dbContext.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Cancelled, cancelledOrder.Status);
            Assert.Contains("Customer requested", cancelledOrder.Notes);
        }

        [Fact]
        public async Task GetCustomerOrders_ReturnsPaginatedResults()
        {
            // Arrange
            var customer = await _dbContext.Customers.FirstAsync();
            var product = await _dbContext.Products.FirstAsync();

            // Create multiple orders
            for (int i = 0; i < 5; i++)
            {
                var order = new Order(customer.Id);
                order.AddItem(product.Id, product.Name, 1, product.Price);
                _dbContext.Orders.Add(order);
            }
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync($"/api/orders/customer/{customer.Id}?pageNumber=1&pageSize=3");

            // Assert
            response.EnsureSuccessStatusCode();

            var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
            Assert.NotNull(orders);
            Assert.Equal(3, orders.Count);
        }

        [Fact]
        public async Task OrderLifecycle_CompleteFlow_WorksCorrectly()
        {
            // Arrange
            var customer = await _dbContext.Customers.FirstAsync();
            var product = await _dbContext.Products.FirstAsync();

            // 1. Create order
            var createResponse = await _client.PostAsJsonAsync("/api/orders", new
            {
                customerId = customer.Id,
                items = new[]
                {
                    new { productId = product.Id, quantity = 1 }
                }
            });
            createResponse.EnsureSuccessStatusCode();
            var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

            // 2. Verify order created
            var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
            getResponse.EnsureSuccessStatusCode();
            var orderDto = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal("Pending", orderDto.Status);

            // 3. Update to paid
            var paidResponse = await _client.PutAsJsonAsync($"/api/orders/{orderId}/status", new
            {
                status = "Paid",
                notes = "Payment processed"
            });
            paidResponse.EnsureSuccessStatusCode();

            // 4. Verify payment
            var paidOrderResponse = await _client.GetAsync($"/api/orders/{orderId}");
            var paidOrder = await paidOrderResponse.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal("Paid", paidOrder.Status);

            // 5. Update to shipped
            var shippedResponse = await _client.PutAsJsonAsync($"/api/orders/{orderId}/status", new
            {
                status = "Shipped",
                notes = "Shipped via express"
            });
            shippedResponse.EnsureSuccessStatusCode();

            // 6. Verify shipping
            var shippedOrderResponse = await _client.GetAsync($"/api/orders/{orderId}");
            var shippedOrder = await shippedOrderResponse.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal("Shipped", shippedOrder.Status);
        }
    }
}