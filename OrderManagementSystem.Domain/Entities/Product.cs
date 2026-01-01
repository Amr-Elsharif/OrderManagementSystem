using OrderManagementSystem.Domain.Common;
using OrderManagementSystem.Domain.Events.Products;
using OrderManagementSystem.Domain.Exceptions;

namespace OrderManagementSystem.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Sku { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int StockQuantity { get; private set; }
        public int MinStockThreshold { get; private set; }
        public bool IsActive { get; private set; }
        public string Category { get; private set; } = string.Empty;

        private Product() { }

        public Product(string name, string description, string sku, decimal price, int stockQuantity, string category, string createdBy = "")
        {
            SetName(name);
            SetDescription(description);
            SetSku(sku);
            SetPrice(price);
            SetStockQuantity(stockQuantity);
            SetCategory(category);
            IsActive = true;
            MinStockThreshold = 10;

            if (!string.IsNullOrEmpty(createdBy))
                CreatedBy = createdBy;

            if (stockQuantity <= MinStockThreshold)
            {
                AddDomainEvent(new ProductLowStockEvent(Id, Name, stockQuantity));
            }
        }

        public void UpdateDetails(string name, string description, decimal price, string category)
        {
            SetName(name);
            SetDescription(description);
            SetPrice(price);
            SetCategory(category);
            UpdateTimestamps();
        }

        public void ReduceStock(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than zero");

            if (quantity > StockQuantity)
                throw new DomainException($"Insufficient stock. Available: {StockQuantity}, Requested: {quantity}");

            StockQuantity -= quantity;
            UpdateTimestamps();

            if (StockQuantity <= MinStockThreshold)
            {
                AddDomainEvent(new ProductLowStockEvent(Id, Name, StockQuantity));
            }
        }

        public void IncreaseStock(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than zero");

            StockQuantity += quantity;
            UpdateTimestamps();
        }

        public void Activate()
        {
            IsActive = true;
            UpdateTimestamps();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamps();
        }

        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Product name cannot be empty");

            Name = name.Trim();
        }

        private void SetDescription(string description)
        {
            Description = description?.Trim() ?? string.Empty;
        }

        private void SetSku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new DomainException("SKU cannot be empty");

            Sku = sku.Trim().ToUpper();
        }

        private void SetPrice(decimal price)
        {
            if (price < 0)
                throw new DomainException("Price cannot be negative");

            Price = price;
        }

        private void SetStockQuantity(int quantity)
        {
            if (quantity < 0)
                throw new DomainException("Stock quantity cannot be negative");

            StockQuantity = quantity;
        }

        private void SetCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new DomainException("Category cannot be empty");

            Category = category.Trim();
        }
    }
}