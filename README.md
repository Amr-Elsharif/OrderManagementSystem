# OrderManagementSystem 🚀

## 📋 Project Overview

**OrderManagementSystem** is a comprehensive, production-ready backend system built with .NET 9 that demonstrates modern enterprise architecture patterns. This system implements a full-featured order and inventory management solution with a focus on scalability, maintainability, and clean code practices.

> **Perfect for showcasing Senior .NET Developer skills** with implementations of DDD, CQRS, Redis, RabbitMQ, FluentValidation and modern .NET practices.

## ✨ Key Features

| Feature | Description | Technology Used |
|---------|------------|-----------------|
| **Order Processing** | Complete order lifecycle management | DDD, EF Core, SQL Server |
| **Inventory Management** | Real-time stock tracking with alerts | Domain Events, Redis |
| **CQRS Architecture** | Command-Query Responsibility Segregation | MediatR, AutoMapper |
| **Event-Driven Messaging** | Asynchronous event processing | RabbitMQ, MassTransit |
| **Performance Caching** | High-performance data access | Redis, Distributed Caching |
| **Validation & Security** | Input validation and business rules | FluentValidation, DDD Guards |
| **Testing Suite** | Comprehensive test coverage | xUnit, Moq, Integration Tests |

## 🏗️ Architecture Overview

### Clean Architecture Layers
```
OrderManagementSystem/
├── Domain Layer (Core Business Logic)
│   ├── Entities (Order, Product, Customer)
│   ├── Value Objects (Money, Address)
│   ├── Domain Events (OrderCreated, OrderPaid)
│   └── Repository Interfaces
├── Application Layer (Use Cases)
│   ├── Commands & Queries (CQRS)
│   ├── DTOs & Mappings
│   ├── Validators
│   └── Application Services
├── Infrastructure Layer (External Concerns)
│   ├── Data Persistence (EF Core)
│   ├── Messaging (RabbitMQ)
│   ├── Caching (Redis)
│   └── External Services
└── API Layer (Presentation)
    ├── RESTful Controllers
    ├── Middleware
    └── Dependency Injection
```

### Technology Stack

| Layer | Technologies |
|-------|--------------|
| **Framework** | .NET 9, ASP.NET Core Web API |
| **Database** | SQL Server 2022, Entity Framework Core 9 |
| **Caching** | Redis, StackExchange.Redis |
| **Messaging** | RabbitMQ, MassTransit |
| **Architecture** | Clean Architecture, DDD, CQRS |
| **Validation** | FluentValidation |
| **Testing** | xUnit, Moq, Integration Testing |
| **Containerization** | Docker, Docker Compose |

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (Optional)
- [Postman](https://www.postman.com/downloads/) (Optional)

### Installation & Setup

#### 1. Clone the Repository
```bash
git clone https://github.com/Amr-Elsharif/OrderManagementSystem.git
cd OrderManagementSystem
```

#### 2. Start Infrastructure Services
```bash
docker-compose up -d
```
This starts:
- ✅ **SQL Server** on port 1433
- ✅ **RabbitMQ Management** on port 15672
- ✅ **Redis** on port 6379
- ✅ **Redis Commander** on port 8081

#### 3. Configure Application Settings
Update `appsettings.json` with your connection strings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=OrderManagementSystem;Integrated Security=True;TrustServerCertificate=true;",
    "Redis": "localhost:6379"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "VirtualHost": "/",
    "Username": "admin",
    "Password": "admin123"
  }
}
```

#### 4. Apply Database Migrations
```bash
dotnet ef database update --project src/OrderManagementSystem.Infrastructure --startup-project src/OrderManagementSystem.API
```

#### 5. Run the Application
```bash
dotnet run --project src/OrderManagementSystem.API
```

#### 6. Access Services
- **API Swagger UI**: http://localhost:5000/swagger
- **RabbitMQ Management**: http://localhost:15672 (admin/admin123)
- **Redis Commander**: http://localhost:8081

## 📊 API Endpoints

### Orders Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/orders/{id}` | Get order by ID |
| `GET` | `/api/orders/customer/{customerId}` | Get customer orders |
| `POST` | `/api/orders` | Create new order |
| `PUT` | `/api/orders/{id}/status` | Update order status |
| `GET` | `/api/orders/status/{status}` | Get orders by status |

### Products Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/products` | Get all products |
| `GET` | `/api/products/{id}` | Get product by ID |
| `POST` | `/api/products` | Create new product |
| `PUT` | `/api/products/{id}` | Update product |
| `DELETE` | `/api/products/{id}` | Delete product |

### Customers Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/customers` | Get all customers |
| `GET` | `/api/customers/{id}` | Get customer by ID |
| `POST` | `/api/customers` | Create new customer |
| `PUT` | `/api/customers/{id}` | Update customer |

## 💻 Code Examples

### Creating an Order (CQRS Pattern)
```csharp
// Command
public class CreateOrderCommand : IRequest<Guid>
{
    public Guid CustomerId { get; init; }
    public List<OrderItemDto> Items { get; init; }
}

// Handler
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Business logic and domain event publishing
        var order = new Order(request.CustomerId, items);
        
        // Repository pattern
        await _orderRepository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Domain event publishing
        await _messagePublisher.PublishAsync(new OrderCreatedEvent(order.Id));
        
        return order.Id;
    }
}
```

### Domain Event Consumer (RabbitMQ)
```csharp
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        
        // Process domain event asynchronously
        await _emailService.SendOrderConfirmationAsync(message.OrderId);
        await _inventoryService.UpdateStockAsync(message.OrderId);
        
        _logger.LogInformation($"Order {message.OrderId} processed successfully");
    }
}
```

## 🧪 Running Tests

### Unit Tests
```bash
dotnet test tests/OrderManagementSystem.Tests
```

### Integration Tests
```bash
dotnet test tests/OrderManagementSystem.IntegrationTests
```

### Test Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 🔧 Development

### Project Structure
```bash
OrderManagementSystem/
├── src
│   ├── OrderManagementSystem.API/
│   ├── OrderManagementSystem.Application/
│   ├── OrderManagementSystem.Domain/
│   └── OrderManagementSystem.Infrastructure/
├── tests/
│   └── OrderManagementSystem.Tests/
├── docker-compose.yml
├── .github/
│   └── workflows/
│       └── ci-cd.yml
└── README.md
```

### Common Commands

| Command | Purpose |
|---------|---------|
| `dotnet build` | Build the solution |
| `dotnet test` | Run all tests |
| `dotnet ef migrations add [name]` | Create new migration |
| `dotnet ef database update` | Apply migrations |
| `docker-compose up -d` | Start infrastructure |

## 🐳 Docker Deployment

### Build Docker Image
```bash
docker build -t ordermanagement-api -f src/OrderManagementSystem.API/Dockerfile .
```

### Run with Docker Compose
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Docker Compose Services
```yaml
services:
  api:
    image: ordermanagement-api:latest
    depends_on:
      - sqlserver
      - redis
      - rabbitmq
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=OrderManagementSystem;User Id=sa;Password=Your_password123
      - ConnectionStrings__Redis=redis:6379
    ports:
      - "8080:80"
```

## 🔍 Monitoring & Observability

### Health Checks
```
GET /health          # Overall system health
GET /health/ready    # Readiness probe
GET /health/live     # Liveness probe
```

### Logging
- Structured logging with Serilog
- Console, File, and Seq sinks
- Request correlation IDs
- Log levels: Debug, Info, Warning, Error

### Metrics
- Request/response timing
- Cache hit/miss rates
- Database query performance
- Message queue statistics

## 📈 Performance Optimizations

1. **Redis Caching**: Frequently accessed data cached with configurable TTL
2. **Database Indexing**: Optimized queries with proper indexes
3. **Connection Pooling**: Efficient database connection management
4. **Message Batching**: Optimized RabbitMQ message handling
5. **Lazy Loading**: EF Core navigation property optimization

## 🛡️ Security Features

- Input validation with FluentValidation
- SQL injection prevention (parameterized queries)
- CORS configuration
- HTTPS enforcement (production)
- API key authentication (extensible)
- Rate limiting (implementable)
- Audit logging

## 📚 Learning Resources

### Concepts Implemented
- [Domain-Driven Design (DDD)](https://domainlanguage.com/ddd/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Event-Driven Architecture](https://aws.amazon.com/event-driven-architecture/)
- [Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

### .NET Resources
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Entity Framework Core 8](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Web API](https://docs.microsoft.com/en-us/aspnet/core/web-api/)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Development Guidelines
- Follow Clean Architecture principles
- Write tests for new features
- Update documentation
- Use meaningful commit messages
- Follow C# coding conventions

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Your Name**
- GitHub: [@yourusername](https://github.com/yourusername)
- LinkedIn: [Your Name](https://www.linkedin.com/in/yourprofile/)
- Email: your.email@example.com

## 🙏 Acknowledgments

- Inspired by Clean Architecture by Robert C. Martin
- Domain-Driven Design concepts by Eric Evans
- .NET community for excellent libraries and tools

## 🚀 Roadmap

- [ ] Add authentication with JWT
- [ ] Implement GraphQL endpoint
- [ ] Add gRPC services
- [ ] Implement API versioning
- [ ] Add comprehensive monitoring with Prometheus/Grafana
- [ ] Implement WebSocket for real-time updates
- [ ] Add export functionality (PDF, Excel)
- [ ] Implement advanced search with Elasticsearch

---

<div align="center">

### ⭐️ Show Your Support

If you found this project useful, please give it a star! ⭐️

**Built with ❤️ and modern .NET technologies**

</div>