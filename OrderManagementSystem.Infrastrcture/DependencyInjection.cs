using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Application.Mappings;
using OrderManagementSystem.Application.Options;
using OrderManagementSystem.Infrastructure.Caching;
using OrderManagementSystem.Infrastructure.Data;
using OrderManagementSystem.Infrastructure.Messaging;
using OrderManagementSystem.Infrastructure.Messaging.Order;
using OrderManagementSystem.Infrastructure.Messaging.Orders;
using OrderManagementSystem.Infrastructure.Messaging.Product;
using OrderManagementSystem.Infrastructure.Messaging.Products;
using OrderManagementSystem.Infrastructure.Repositories;
using OrderManagementSystem.Infrastructure.Services;

namespace OrderManagementSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();

            // Redis Caching
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = "OrderManagementSystem_";
            });

            services.AddScoped<ICacheService, RedisCacheService>();

            // RabbitMQ with MassTransit
            services.AddMassTransit(x =>
            {
                // Order consumers
                x.AddConsumer<OrderCreatedConsumer>();
                x.AddConsumer<OrderPaidConsumer>();
                x.AddConsumer<OrderShippedConsumer>();
                x.AddConsumer<OrderCancelledConsumer>();
                x.AddConsumer<OrderCompletedConsumer>();

                // Order item consumers
                x.AddConsumer<OrderItemAddedConsumer>();
                x.AddConsumer<OrderItemRemovedConsumer>();
                x.AddConsumer<OrderItemQuantityUpdatedConsumer>();

                // Product consumers
                x.AddConsumer<ProductCreatedConsumer>();
                x.AddConsumer<ProductUpdatedConsumer>();
                x.AddConsumer<ProductDeletedConsumer>();
                x.AddConsumer<ProductLowStockConsumer>();
                x.AddConsumer<ProductStockUpdatedConsumer>();
                x.AddConsumer<ProductPriceChangedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(
                        configuration["RabbitMQ:Host"],
                        configuration["RabbitMQ:VirtualHost"],
                        h =>
                        {
                            h.Username(configuration["RabbitMQ:Username"]);
                            h.Password(configuration["RabbitMQ:Password"]);
                        });

                    // Order queues
                    cfg.ReceiveEndpoint("order-created", e =>
                    {
                        e.ConfigureConsumer<OrderCreatedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("order-paid", e =>
                    {
                        e.ConfigureConsumer<OrderPaidConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("order-shipped", e =>
                    {
                        e.ConfigureConsumer<OrderShippedConsumer>(context);
                        e.ConfigureConsumer<OrderCompletedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("order-cancelled", e =>
                    {
                        e.ConfigureConsumer<OrderCancelledConsumer>(context);
                    });

                    // Order item queues
                    cfg.ReceiveEndpoint("order-item-added", e =>
                    {
                        e.ConfigureConsumer<OrderItemAddedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("order-item-removed", e =>
                    {
                        e.ConfigureConsumer<OrderItemRemovedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("order-item-quantity-updated", e =>
                    {
                        e.ConfigureConsumer<OrderItemQuantityUpdatedConsumer>(context);
                    });

                    // Product queues
                    cfg.ReceiveEndpoint("product-created", e =>
                    {
                        e.ConfigureConsumer<ProductCreatedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("product-updated", e =>
                    {
                        e.ConfigureConsumer<ProductUpdatedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("product-deleted", e =>
                    {
                        e.ConfigureConsumer<ProductDeletedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("product-low-stock", e =>
                    {
                        e.ConfigureConsumer<ProductLowStockConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("product-stock-updated", e =>
                    {
                        e.ConfigureConsumer<ProductStockUpdatedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("product-price-changed", e =>
                    {
                        e.ConfigureConsumer<ProductPriceChangedConsumer>(context);
                    });
                });
            });

            services.AddScoped<IMessagePublisher, RabbitMQPublisher>();

            // Services
            services.AddScoped<IEmailService, EmailService>();

            // AutoMapper
            services.AddAutoMapper(typeof(MappingProfile));

            // Email services
            services.Configure<EmailOptions>(options =>
            {
                options.SmtpServer = configuration["Email:SmtpServer"];
                options.SmtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
                options.EnableSsl = bool.Parse(configuration["Email:EnableSsl"] ?? "true");
                options.Username = configuration["Email:Username"] ?? string.Empty;
                options.Password = configuration["Email:Password"] ?? string.Empty;
                options.FromEmail = configuration["Email:FromEmail"] ?? string.Empty;
                options.FromName = configuration["Email:FromName"] ?? string.Empty;
                options.BccEmail = configuration["Email:BccEmail"] ?? string.Empty;
                options.IsDevelopmentMode = bool.Parse(configuration["Email:IsDevelopmentMode"] ?? "true");
                options.SendEmailsInDevelopment = bool.Parse(configuration["Email:SendEmailsInDevelopment"] ?? "false");
                options.MaxRetryAttempts = int.Parse(configuration["Email:MaxRetryAttempts"] ?? "3");
                options.TimeoutMilliseconds = int.Parse(configuration["Email:TimeoutMilliseconds"] ?? "30000");

                // Parse AdminEmails from comma-separated string
                var adminEmails = configuration["Email:AdminEmails"] ?? string.Empty;
                options.AdminEmails = [.. adminEmails.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(email => email.Trim())];

                // Parse FinanceEmails from comma-separated string
                var financeEmails = configuration["Email:FinanceEmails"] ?? string.Empty;
                options.FinanceEmails = [.. financeEmails.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(email => email.Trim())];
            });


            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();

            return services;
        }
    }
}