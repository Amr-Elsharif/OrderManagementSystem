namespace OrderManagementSystem.Application.Models.Emails
{
    public class CancellationNotificationEmail : BaseEmailModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public DateTime CancellationDate { get; set; }
        public string? RefundReference { get; set; }
    }
}
