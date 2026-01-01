namespace OrderManagementSystem.Application.Models.Emails
{
    public class StockOutAlertEmail : BaseEmailModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int LastStockCount { get; set; }
        public DateTime LastRestockDate { get; set; }
        public string? SupplierName { get; set; }
    }
}
