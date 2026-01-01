using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Infrastructure.Data;

namespace OrderManagementSystem.Tests.IntegrationTests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestDb");
                });

                // Build service provider
                var sp = services.BuildServiceProvider();

                // Create scope and seed database
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                    db.Database.EnsureCreated();

                    try
                    {
                        // Seed test data
                        SeedTestData(db);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding test database: {ex.Message}");
                    }
                }
            });
        }

        private void SeedTestData(ApplicationDbContext context)
        {
            // Clear existing data
            context.Orders.RemoveRange(context.Orders);
            context.Products.RemoveRange(context.Products);
            context.Customers.RemoveRange(context.Customers);
            context.SaveChanges();

            // Seed customers
            var customers = new List<Customer>
            {
                new("John", "Doe", "john.doe@test.com", "1234567890", "123 Test St"),
                new("Jane", "Smith", "jane.smith@test.com", "0987654321", "456 Test Ave"),
                new("Bob", "Johnson", "bob.johnson@test.com", "5551234567", "789 Test Blvd")
            };
            context.Customers.AddRange(customers);

            // Seed products
            var products = new List<Product>
            {
                new("Laptop", "High-performance laptop", "LAP-001", 999.99m, 10, "Electronics"),
                new("Mouse", "Wireless mouse", "MOU-001", 29.99m, 50, "Electronics"),
                new("Keyboard", "Mechanical keyboard", "KEY-001", 89.99m, 30, "Electronics"),
                new("Monitor", "27-inch 4K monitor", "MON-001", 399.99m, 15, "Electronics"),
                new("Desk", "Office desk", "DES-001", 199.99m, 5, "Furniture"),
                new("Chair", "Ergonomic chair", "CHA-001", 299.99m, 8, "Furniture"),
                new("Headphones", "Noise-cancelling headphones", "HPH-001", 149.99m, 20, "Electronics"),
                new("Tablet", "10-inch tablet", "TAB-001", 249.99m, 12, "Electronics"),
                new("Phone", "Smartphone", "PHO-001", 699.99m, 25, "Electronics"),
                new("Speaker", "Bluetooth speaker", "SPK-001", 79.99m, 40, "Electronics")
            };
            context.Products.AddRange(products);

            context.SaveChanges();

            // Seed some orders
            var john = customers[0];
            var laptop = products[0];
            var mouse = products[1];

            var order1 = new Order(john.Id);
            order1.AddItem(laptop.Id, laptop.Name, 1, laptop.Price);
            order1.AddItem(mouse.Id, mouse.Name, 2, mouse.Price);
            order1.MarkAsPaid("CreditCard", order1.TotalAmount.Amount);

            var order2 = new Order(john.Id);
            order2.AddItem(products[2].Id, products[2].Name, 1, products[2].Price);

            context.Orders.AddRange(order1, order2);
            context.SaveChanges();
        }
    }
}