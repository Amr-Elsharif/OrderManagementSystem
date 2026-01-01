using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using OrderManagementSystem.Application.DTOs.Common;
using OrderManagementSystem.Application.DTOs.Products;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Infrastructure.Data;
using System.Net.Http.Json;

namespace OrderManagementSystem.Tests.IntegrationTests
{
    public class ProductIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ApplicationDbContext _dbContext;

        public ProductIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("ProductManagementTestDb");
                    });
                });
            });

            _client = _factory.CreateClient();

            var scope = _factory.Services.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _dbContext.Database.EnsureCreated();
            SeedTestData();
        }

        private void SeedTestData()
        {
            _dbContext.Products.RemoveRange(_dbContext.Products);

            var products = new List<Product>
            {
                new("Laptop", "High-performance laptop", "LAP-001", 999.99m, 10, "Electronics"),
                new("Mouse", "Wireless mouse", "MOU-001", 29.99m, 50, "Electronics"),
                new("Keyboard", "Mechanical keyboard", "KEY-001", 89.99m, 30, "Electronics"),
                new("Desk", "Office desk", "DES-001", 199.99m, 5, "Furniture"),
                new("Chair", "Ergonomic chair", "CHA-001", 299.99m, 8, "Furniture")
            };

            _dbContext.Products.AddRange(products);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task CreateProduct_ValidRequest_ReturnsCreatedProduct()
        {
            // Arrange
            var productData = new
            {
                name = "New Test Product",
                description = "Test Description",
                sku = "TEST-999",
                price = 49.99m,
                stockQuantity = 100,
                category = "Test Category"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/products", productData);

            // Assert
            response.EnsureSuccessStatusCode();
            var productId = await response.Content.ReadFromJsonAsync<Guid>();

            Assert.NotEqual(Guid.Empty, productId);

            // Verify product was created
            var product = await _dbContext.Products.FindAsync(productId);
            Assert.NotNull(product);
            Assert.Equal("New Test Product", product.Name);
            Assert.Equal("TEST-999", product.Sku);
        }

        [Fact]
        public async Task CreateProduct_DuplicateSku_ReturnsError()
        {
            // Arrange
            var existingProduct = await _dbContext.Products.FirstAsync();

            var productData = new
            {
                name = "Different Product",
                description = "Different Description",
                sku = existingProduct.Sku, // Duplicate SKU
                price = 99.99m,
                stockQuantity = 50,
                category = "Electronics"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/products", productData);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

            var error = await response.Content.ReadAsStringAsync();
            Assert.Contains("already exists", error);
        }

        [Fact]
        public async Task GetProduct_ValidId_ReturnsProduct()
        {
            // Arrange
            var expectedProduct = await _dbContext.Products.FirstAsync();

            // Act
            var response = await _client.GetAsync($"/api/products/{expectedProduct.Id}");

            // Assert
            response.EnsureSuccessStatusCode();

            var productDto = await response.Content.ReadFromJsonAsync<ProductDto>();
            Assert.NotNull(productDto);
            Assert.Equal(expectedProduct.Id, productDto.Id);
            Assert.Equal(expectedProduct.Name, productDto.Name);
            Assert.Equal(expectedProduct.Sku, productDto.Sku);
        }

        [Fact]
        public async Task UpdateProduct_ValidRequest_UpdatesSuccessfully()
        {
            // Arrange
            var product = await _dbContext.Products.FirstAsync();

            var updateData = new
            {
                name = "Updated Product Name",
                description = "Updated Description",
                price = 149.99m,
                category = "Updated Category"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/products/{product.Id}", updateData);

            // Assert
            response.EnsureSuccessStatusCode();

            // Verify update
            var updatedProduct = await _dbContext.Products.FindAsync(product.Id);
            Assert.Equal("Updated Product Name", updatedProduct.Name);
            Assert.Equal(149.99m, updatedProduct.Price);
            Assert.Equal("Updated Category", updatedProduct.Category);
        }

        [Fact]
        public async Task UpdateProductStock_IncreaseStock_UpdatesSuccessfully()
        {
            // Arrange
            var product = await _dbContext.Products.FirstAsync();
            var initialStock = product.StockQuantity;

            var stockData = new
            {
                quantityChange = 25,
                reason = "Restocked from supplier"
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/products/{product.Id}/stock", stockData);

            // Assert
            response.EnsureSuccessStatusCode();

            var updatedProduct = await _dbContext.Products.FindAsync(product.Id);
            Assert.Equal(initialStock + 25, updatedProduct.StockQuantity);
        }

        [Fact]
        public async Task UpdateProductStock_DecreaseStock_UpdatesSuccessfully()
        {
            // Arrange
            var product = await _dbContext.Products.FirstAsync();
            var initialStock = product.StockQuantity;

            var stockData = new
            {
                quantityChange = -5,
                reason = "Sold in store"
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/products/{product.Id}/stock", stockData);

            // Assert
            response.EnsureSuccessStatusCode();

            var updatedProduct = await _dbContext.Products.FindAsync(product.Id);
            Assert.Equal(initialStock - 5, updatedProduct.StockQuantity);
        }

        [Fact]
        public async Task UpdateProductStock_InsufficientStock_ReturnsError()
        {
            // Arrange
            var product = await _dbContext.Products.FirstAsync();
            var initialStock = product.StockQuantity;

            // Try to reduce more than available
            var stockData = new
            {
                quantityChange = -(initialStock + 10), // More than available
                reason = "Test reduction"
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/products/{product.Id}/stock", stockData);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

            var error = await response.Content.ReadAsStringAsync();
            Assert.Contains("Insufficient stock", error);
        }

        [Fact]
        public async Task DeleteProduct_SoftDelete_DeactivatesProduct()
        {
            // Arrange
            var product = await _dbContext.Products.FirstAsync();
            Assert.True(product.IsActive);

            // Act
            var response = await _client.DeleteAsync($"/api/products/{product.Id}?softDelete=true");

            // Assert
            response.EnsureSuccessStatusCode();

            var deletedProduct = await _dbContext.Products.FindAsync(product.Id);
            Assert.False(deletedProduct.IsActive);
        }

        [Fact]
        public async Task DeleteProduct_WithActiveOrders_ReturnsError()
        {
            // Arrange - Create a product with an active order
            var customer = new Customer("Test", "Customer", "test@test.com");
            _dbContext.Customers.Add(customer);

            var product = new Product("Ordered Product", "Description", "ORD-001", 99.99m, 10, "Test");
            _dbContext.Products.Add(product);

            await _dbContext.SaveChangesAsync();

            // Create an order with this product
            var order = new Order(customer.Id);
            order.AddItem(product.Id, product.Name, 1, product.Price);
            _dbContext.Orders.Add(order);

            await _dbContext.SaveChangesAsync();

            // Act - Try to delete the product
            var response = await _client.DeleteAsync($"/api/products/{product.Id}");

            // Assert - Should fail because product has active order
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

            var error = await response.Content.ReadAsStringAsync();
            Assert.Contains("Cannot delete product with active orders", error);
        }

        [Fact]
        public async Task GetLowStockProducts_ReturnsLowStockItems()
        {
            // Arrange
            var lowStockProduct = new Product("Low Stock", "Description", "LOW-001", 19.99m, 3, "Test");
            _dbContext.Products.Add(lowStockProduct);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync("/api/products/low-stock");

            // Assert
            response.EnsureSuccessStatusCode();

            var lowStockProducts = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
            Assert.NotNull(lowStockProducts);
            Assert.Contains(lowStockProducts, p => p.Sku == "LOW-001");
        }

        [Fact]
        public async Task GetProducts_WithFilters_ReturnsFilteredResults()
        {
            // Act - Get only electronics
            var response = await _client.GetAsync("/api/products?category=Electronics&pageSize=100");

            // Assert
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ProductDto>>();
            Assert.NotNull(result);
            Assert.All(result.Items, p => Assert.Equal("Electronics", p.Category));
        }

        [Fact]
        public async Task GetProducts_WithPriceRange_ReturnsFilteredResults()
        {
            // Act - Get products between $50 and $150
            var response = await _client.GetAsync("/api/products?minPrice=50&maxPrice=150&pageSize=100");

            // Assert
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ProductDto>>();
            Assert.NotNull(result);
            Assert.All(result.Items, p =>
            {
                Assert.True(p.Price >= 50);
                Assert.True(p.Price <= 150);
            });
        }

        [Fact]
        public async Task GetProducts_WithSearchTerm_ReturnsMatchingResults()
        {
            // Arrange
            var searchProduct = new Product("Special Search Product", "Unique description",
                "SRCH-001", 79.99m, 10, "Test");
            _dbContext.Products.Add(searchProduct);
            await _dbContext.SaveChangesAsync();

            // Act - Search for "Special Search"
            var response = await _client.GetAsync("/api/products?searchTerm=Special%20Search&pageSize=100");

            // Assert
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ProductDto>>();
            Assert.NotNull(result);
            Assert.Contains(result.Items, p => p.Name.Contains("Special Search"));
        }
    }
}