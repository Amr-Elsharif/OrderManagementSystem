namespace OrderManagementSystem.Application.DTOs.Products
{
    public class UpdateProductStockDto
    {
        public int QuantityChange { get; set; }
        public string? Reason { get; set; }
    }
}
