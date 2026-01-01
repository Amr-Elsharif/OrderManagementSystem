namespace OrderManagementSystem.Application.Models.Emails
{
    public abstract class BaseEmailModel
    {
        public Guid? OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
