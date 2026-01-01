namespace OrderManagementSystem.Application.DTOs.Products
{
    public class ProductStockUpdateDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public int Change { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
