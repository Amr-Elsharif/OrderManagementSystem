using OrderManagementSystem.Application.Models.Emails;

namespace OrderManagementSystem.Application.Interfaces
{
    public interface IEmailService
    {
        // Order-related emails
        Task SendOrderConfirmationAsync(Guid orderId, string customerEmail, CancellationToken cancellationToken = default);
        Task SendOrderConfirmationAsync(OrderConfirmationEmail model, CancellationToken cancellationToken = default);
        Task SendPaymentConfirmationAsync(Guid orderId, string customerEmail, decimal amount, string paymentMethod, CancellationToken cancellationToken = default);
        Task SendShippingNotificationAsync(Guid orderId, string customerEmail, string trackingNumber, CancellationToken cancellationToken = default);
        Task SendCancellationNotificationAsync(Guid orderId, string customerEmail, string reason, CancellationToken cancellationToken = default);
        Task SendOrderStatusUpdateAsync(Guid orderId, string customerEmail, string newStatus, string notes, CancellationToken cancellationToken = default);

        // Product-related emails
        Task SendStockOutAlertAsync(Guid productId, string productName, CancellationToken cancellationToken = default);
        Task SendProductDeletionNotificationAsync(ProductDeletionEmail model, CancellationToken cancellationToken = default);
        Task SendProductDeletionNotificationAsync(Guid productId, string productName, string reason, CancellationToken cancellationToken = default);
        Task SendHighValueProductAlertAsync(HighValueProductEmail model, CancellationToken cancellationToken = default);
        Task SendHighValueProductAlertAsync(Guid productId, string productName, decimal price, string category, CancellationToken cancellationToken = default);
    }
}