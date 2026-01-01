# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["OrderManagementSystem.sln", "./"]
COPY ["src/OrderManagementSystem.API/OrderManagementSystem.API.csproj", "src/OrderManagementSystem.API/"]
COPY ["src/OrderManagementSystem.Application/OrderManagementSystem.Application.csproj", "src/OrderManagementSystem.Application/"]
COPY ["src/OrderManagementSystem.Domain/OrderManagementSystem.Domain.csproj", "src/OrderManagementSystem.Domain/"]
COPY ["src/OrderManagementSystem.Infrastructure/OrderManagementSystem.Infrastructure.csproj", "src/OrderManagementSystem.Infrastructure/"]

# Restore packages
RUN dotnet restore "OrderManagementSystem.sln"

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/src/OrderManagementSystem.API"
RUN dotnet publish "OrderManagementSystem.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=build /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser:appuser /app
USER appuser

# Expose ports
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "OrderManagementSystem.API.dll"]