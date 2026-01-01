namespace OrderManagementSystem.Application.Models.Emails
{
    public class ShippingNotificationEmail : BaseEmailModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string ShippingCarrier { get; set; } = "Standard Shipping";
        public AddressEmailModel ShippingAddress { get; set; } = new();
        public DateTime? EstimatedDelivery { get; set; }
    }
}
