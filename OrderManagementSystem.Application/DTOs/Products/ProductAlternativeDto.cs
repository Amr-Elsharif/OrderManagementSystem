namespace OrderManagementSystem.Application.DTOs.Products
{
    public class ProductAlternativeDto
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; }
    }
}
