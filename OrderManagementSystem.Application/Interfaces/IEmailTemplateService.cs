using OrderManagementSystem.Application.Models.Emails;

namespace OrderManagementSystem.Application.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<string> GenerateOrderConfirmationEmailAsync(OrderConfirmationEmail model);
        Task<string> GeneratePaymentConfirmationEmailAsync(PaymentConfirmationEmail model);
        Task<string> GenerateShippingNotificationEmailAsync(ShippingNotificationEmail model);
        Task<string> GenerateCancellationNotificationEmailAsync(CancellationNotificationEmail model);
        Task<string> GenerateOrderStatusUpdateEmailAsync(OrderStatusUpdateEmail model);
        Task<string> GenerateStockOutAlertEmailAsync(StockOutAlertEmail model);
        Task<string> GenerateProductDeletionEmailAsync(ProductDeletionEmail model);
        Task<string> GenerateHighValueProductAlertEmailAsync(HighValueProductEmail model);
    }
}
