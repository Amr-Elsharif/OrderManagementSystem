namespace OrderManagementSystem.Application.Models.Emails
{
    public class OrderConfirmationEmail : BaseEmailModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemEmailModel> Items { get; set; } = new();
        public AddressEmailModel ShippingAddress { get; set; } = new();
        public string? TrackingNumber { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
    }
}
