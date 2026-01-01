namespace OrderManagementSystem.Application.Models.Emails
{
    public class OrderStatusUpdateEmail : BaseEmailModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime StatusChangeDate { get; set; }
    }
}
