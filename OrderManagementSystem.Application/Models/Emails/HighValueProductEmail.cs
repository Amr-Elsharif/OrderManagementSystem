namespace OrderManagementSystem.Application.Models.Emails
{
    public class HighValueProductEmail : BaseEmailModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal ProfitMargin { get; set; }
        public int CurrentStock { get; set; }
        public int MonthlySales { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }
}
