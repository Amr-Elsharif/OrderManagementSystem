using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Application.Models.Emails;
using OrderManagementSystem.Application.Options;
using OrderManagementSystem.Domain.Entities;
using OrderManagementSystem.Domain.Enums;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace OrderManagementSystem.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<EmailService> _logger;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IEmailTemplateService _templateService;
        private readonly SmtpClient _smtpClient;

        public EmailService(
            IOptions<EmailOptions> emailOptions,
            ILogger<EmailService> logger,
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            ICustomerRepository customerRepository,
            IEmailTemplateService templateService)
        {
            _emailOptions = emailOptions.Value;
            _logger = logger;
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _templateService = templateService;

            // Configure SMTP client
            _smtpClient = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.SmtpPort)
            {
                EnableSsl = _emailOptions.EnableSsl,
                Credentials = new NetworkCredential(_emailOptions.Username, _emailOptions.Password),
                Timeout = _emailOptions.TimeoutMilliseconds
            };

            ValidateEmailConfiguration();
        }

        #region Order-related Emails

        public async Task SendOrderConfirmationAsync(Guid orderId, string customerEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending order confirmation for order {OrderId} to {Email}", orderId, customerEmail);

                var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for email confirmation", orderId);
                    return;
                }

                var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);

                // Generate email model
                var model = new OrderConfirmationEmail
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    CustomerName = GetCustomerDisplayName(customer),
                    CustomerEmail = customer?.Email ?? customerEmail,
                    OrderDate = order.CreatedAt,
                    TotalAmount = order.TotalAmount.Amount,
                    Items = await GetOrderItemsForEmailAsync(order.Items, cancellationToken),
                    ShippingAddress = ParseShippingAddress(order.ShippingAddress),
                    TrackingNumber = null, // Will be set when shipped
                    EstimatedDelivery = CalculateEstimatedDelivery(order.CreatedAt, order.Status)
                };

                await SendOrderConfirmationAsync(model, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation for order {OrderId}", orderId);
                throw new ApplicationException($"Failed to send order confirmation for order {orderId}", ex);
            }
        }

        public async Task SendOrderConfirmationAsync(OrderConfirmationEmail model, CancellationToken cancellationToken = default)
        {
            try
            {
                var subject = $"Order Confirmation - {model.OrderNumber}";
                var body = await _templateService.GenerateOrderConfirmationEmailAsync(model);

                await SendEmailAsync(
                    to: model.CustomerEmail,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Order confirmation sent successfully for order {OrderId}", model.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation for order {OrderId}", model.OrderId);
                throw;
            }
        }

        public async Task SendPaymentConfirmationAsync(Guid orderId, string customerEmail, decimal amount, string paymentMethod, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending payment confirmation for order {OrderId}", orderId);

                var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for payment confirmation", orderId);
                    return;
                }

                var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);

                var model = new PaymentConfirmationEmail
                {
                    OrderId = orderId,
                    OrderNumber = order.OrderNumber,
                    CustomerName = GetCustomerDisplayName(customer),
                    CustomerEmail = customer?.Email ?? customerEmail,
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    TransactionId = $"TXN-{Guid.NewGuid():N}".Substring(0, 12).ToUpper(),
                    PaymentDate = DateTime.UtcNow
                };

                var subject = $"Payment Confirmation - {order.OrderNumber}";
                var body = await _templateService.GeneratePaymentConfirmationEmailAsync(model);

                await SendEmailAsync(
                    to: customerEmail,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Payment confirmation sent successfully for order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment confirmation for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task SendShippingNotificationAsync(Guid orderId, string customerEmail, string trackingNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending shipping notification for order {OrderId}", orderId);

                var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for shipping notification", orderId);
                    return;
                }

                var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);

                var model = new ShippingNotificationEmail
                {
                    OrderId = orderId,
                    OrderNumber = order.OrderNumber,
                    CustomerName = GetCustomerDisplayName(customer),
                    CustomerEmail = customer?.Email ?? customerEmail,
                    TrackingNumber = trackingNumber,
                    ShippingCarrier = "Standard Shipping",
                    ShippingAddress = ParseShippingAddress(order.ShippingAddress),
                    EstimatedDelivery = CalculateEstimatedDelivery(order.CreatedAt, order.Status)
                };

                var subject = $"Your Order {order.OrderNumber} Has Been Shipped!";
                var body = await _templateService.GenerateShippingNotificationEmailAsync(model);

                await SendEmailAsync(
                    to: customerEmail,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Shipping notification sent successfully for order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending shipping notification for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task SendCancellationNotificationAsync(Guid orderId, string customerEmail, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending cancellation notification for order {OrderId}", orderId);

                var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for cancellation notification", orderId);
                    return;
                }

                var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);

                var model = new CancellationNotificationEmail
                {
                    OrderId = orderId,
                    OrderNumber = order.OrderNumber,
                    CustomerName = GetCustomerDisplayName(customer),
                    CustomerEmail = customer?.Email ?? customerEmail,
                    Reason = reason,
                    RefundAmount = order.TotalAmount.Amount,
                    CancellationDate = DateTime.UtcNow,
                    RefundReference = $"REF-{Guid.NewGuid():N}".Substring(0, 10).ToUpper()
                };

                var subject = $"Order Cancellation - {order.OrderNumber}";
                var body = await _templateService.GenerateCancellationNotificationEmailAsync(model);

                await SendEmailAsync(
                    to: customerEmail,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Cancellation notification sent successfully for order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending cancellation notification for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task SendOrderStatusUpdateAsync(Guid orderId, string customerEmail, string newStatus, string notes, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending order status update for order {OrderId}", orderId);

                var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for status update", orderId);
                    return;
                }

                var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);

                var model = new OrderStatusUpdateEmail
                {
                    OrderId = orderId,
                    OrderNumber = order.OrderNumber,
                    CustomerName = GetCustomerDisplayName(customer),
                    CustomerEmail = customer?.Email ?? customerEmail,
                    OldStatus = order.Status.ToString(),
                    NewStatus = newStatus,
                    Notes = notes,
                    StatusChangeDate = DateTime.UtcNow
                };

                var subject = $"Order Status Update - {order.OrderNumber}";
                var body = await _templateService.GenerateOrderStatusUpdateEmailAsync(model);

                await SendEmailAsync(
                    to: customerEmail,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Order status update sent successfully for order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order status update for order {OrderId}", orderId);
                throw;
            }
        }

        #endregion

        #region Product-related Emails

        public async Task SendStockOutAlertAsync(Guid productId, string productName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending stock out alert for product {ProductId}", productId);

                var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for stock alert", productId);
                    return;
                }

                // Using your Product entity properties
                var model = new StockOutAlertEmail
                {
                    ProductId = productId,
                    ProductName = !string.IsNullOrEmpty(productName) ? productName : product.Name,
                    Sku = product.Sku,
                    Category = product.Category,
                    Price = product.Price,
                    LastStockCount = product.StockQuantity,
                    LastRestockDate = DateTime.UtcNow.AddDays(-30), // Default fallback since no LastRestockedDate property
                    SupplierName = "Unknown Supplier" // Fallback since no Supplier property
                };

                var subject = $"🚨 Stock Alert: {product.Name} is Out of Stock";
                var body = await _templateService.GenerateStockOutAlertEmailAsync(model);

                // Send to admin emails
                foreach (var adminEmail in _emailOptions.AdminEmails)
                {
                    await SendEmailAsync(
                        to: adminEmail,
                        subject: subject,
                        body: body,
                        isHtml: true,
                        cancellationToken: cancellationToken
                    );
                }

                _logger.LogInformation("Stock out alert sent successfully for product {ProductId}", productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending stock out alert for product {ProductId}", productId);
                throw;
            }
        }

        public async Task SendProductDeletionNotificationAsync(ProductDeletionEmail model, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending product deletion notification for product {ProductId}", model.ProductId);

                var subject = $"Product Deleted: {model.ProductName}";
                var body = await _templateService.GenerateProductDeletionEmailAsync(model);

                // Send to admin emails
                foreach (var adminEmail in _emailOptions.AdminEmails)
                {
                    await SendEmailAsync(
                        to: adminEmail,
                        subject: subject,
                        body: body,
                        isHtml: true,
                        cancellationToken: cancellationToken
                    );
                }

                _logger.LogInformation("Product deletion notification sent successfully for product {ProductId}", model.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending product deletion notification for product {ProductId}", model.ProductId);
                throw;
            }
        }

        public async Task SendProductDeletionNotificationAsync(Guid productId, string productName, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending product deletion notification for product {ProductId}", productId);

                // Get product details if available
                var product = await _productRepository.GetByIdAsync(productId, cancellationToken);

                // Create the model
                var model = new ProductDeletionEmail
                {
                    ProductId = productId,
                    ProductName = productName,
                    Sku = product?.Sku ?? "N/A",
                    Reason = reason,
                    DeletedBy = "System", // You can pass this as a parameter if needed
                    DeletionDate = DateTime.UtcNow,
                    AffectedOrders = await GetAffectedOrdersAsync(productId, cancellationToken)
                };

                // Call the model-based overload
                await SendProductDeletionNotificationAsync(model, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending product deletion notification for product {ProductId}", productId);
                throw;
            }
        }

        public async Task SendHighValueProductAlertAsync(HighValueProductEmail model, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending high value product alert for product {ProductId}", model.ProductId);

                var subject = $"💰 High Value Product Alert: {model.ProductName}";
                var body = await _templateService.GenerateHighValueProductAlertEmailAsync(model);

                // Send to finance/admin emails
                var recipients = _emailOptions.AdminEmails.Concat(_emailOptions.FinanceEmails ?? new List<string>()).Distinct();
                foreach (var recipientEmail in recipients)
                {
                    await SendEmailAsync(
                        to: recipientEmail,
                        subject: subject,
                        body: body,
                        isHtml: true,
                        cancellationToken: cancellationToken
                    );
                }

                _logger.LogInformation("High value product alert sent successfully for product {ProductId}", model.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending high value product alert for product {ProductId}", model.ProductId);
                throw;
            }
        }

        public async Task SendHighValueProductAlertAsync(Guid productId, string productName, decimal price, string category, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending high value product alert for product {ProductId}", productId);

                // Get product details
                var product = await _productRepository.GetByIdAsync(productId, cancellationToken);

                // Calculate metrics
                var monthlySales = await CalculateMonthlySalesAsync(productId, cancellationToken);
                var monthlyRevenue = await CalculateMonthlyRevenueAsync(productId, cancellationToken);

                // Create the model
                var model = new HighValueProductEmail
                {
                    ProductId = productId,
                    ProductName = productName,
                    Sku = product?.Sku ?? "N/A",
                    Price = price,
                    CurrentStock = product?.StockQuantity ?? 0,
                    MonthlySales = monthlySales,
                    MonthlyRevenue = monthlyRevenue,
                    ProfitMargin = product != null ? CalculateProfitMargin(product) : 0
                };

                // Call the model-based overload
                await SendHighValueProductAlertAsync(model, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending high value product alert for product {ProductId}", productId);
                throw;
            }
        }

        private async Task<List<string>> GetAffectedOrdersAsync(Guid productId, CancellationToken cancellationToken)
        {
            try
            {
                // Implement logic to get orders affected by product deletion
                // This is a simplified example
                var affectedOrders = new List<string>();

                // You might query your order repository for orders containing this product
                // For now, return an empty list or placeholder
                affectedOrders.Add("No recent orders found");

                return affectedOrders;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get affected orders for product {ProductId}", productId);
                return new List<string> { "Error retrieving order data" };
            }
        }

        private async Task<int> CalculateMonthlySalesAsync(Guid productId, CancellationToken cancellationToken)
        {
            try
            {
                // Implement actual logic to calculate monthly sales
                // This might involve querying your order repository
                // For now, return a placeholder value
                return new Random().Next(10, 100);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate monthly sales for product {ProductId}", productId);
                return 0;
            }
        }

        private async Task<decimal> CalculateMonthlyRevenueAsync(Guid productId, CancellationToken cancellationToken)
        {
            try
            {
                // Implement actual logic to calculate monthly revenue
                // For now, return a placeholder value
                var monthlySales = await CalculateMonthlySalesAsync(productId, cancellationToken);
                var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                return monthlySales * (product?.Price ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate monthly revenue for product {ProductId}", productId);
                return 0;
            }
        }

        private decimal CalculateProfitMargin(Product product)
        {
            // Simplified profit margin calculation
            // Assuming cost is 60% of selling price
            // You should replace this with actual cost data from your Product entity
            var costPrice = product.Price * 0.6m;
            var profit = product.Price - costPrice;
            return profit / product.Price * 100; // Return percentage
        }
        #endregion

        #region Helper Methods

        private string GetCustomerDisplayName(Customer customer)
        {
            if (customer == null)
                return "Valued Customer";

            // Since we don't know your Customer structure, use reflection to find properties
            // This is flexible and works with any Customer entity structure

            // First, try common property names
            var propertyNames = new[] { "FullName", "Name", "FirstName", "LastName", "DisplayName" };

            foreach (var propName in propertyNames)
            {
                var property = customer.GetType().GetProperty(propName);
                if (property != null)
                {
                    var value = property.GetValue(customer) as string;
                    if (!string.IsNullOrWhiteSpace(value))
                        return value.Trim();
                }
            }

            // If no name property found, try to combine FirstName and LastName
            var firstNameProp = customer.GetType().GetProperty("FirstName");
            var lastNameProp = customer.GetType().GetProperty("LastName");

            if (firstNameProp != null && lastNameProp != null)
            {
                var firstName = firstNameProp.GetValue(customer) as string;
                var lastName = lastNameProp.GetValue(customer) as string;

                if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                    return $"{firstName.Trim()} {lastName.Trim()}";

                if (!string.IsNullOrWhiteSpace(firstName))
                    return firstName.Trim();

                if (!string.IsNullOrWhiteSpace(lastName))
                    return lastName.Trim();
            }

            // Use email username as fallback
            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                var emailName = customer.Email.Split('@').FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(emailName))
                    return emailName;
            }

            return "Valued Customer";
        }

        private async Task<List<OrderItemEmailModel>> GetOrderItemsForEmailAsync(IEnumerable<OrderItem> items, CancellationToken cancellationToken)
        {
            var result = new List<OrderItemEmailModel>();

            if (items == null)
                return result;

            foreach (var item in items)
            {
                string productSku = "N/A";
                string productName = item.ProductName;

                try
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                    if (product != null)
                    {
                        productSku = product.Sku;
                        // Use product name from Product entity if available and item.ProductName is empty
                        if (string.IsNullOrEmpty(productName) && !string.IsNullOrEmpty(product.Name))
                        {
                            productName = product.Name;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch product for item {ItemId}", item.Id);
                }

                result.Add(new OrderItemEmailModel
                {
                    ProductName = productName,
                    Sku = productSku,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                });
            }

            return result;
        }

        private AddressEmailModel ParseShippingAddress(string? shippingAddress)
        {
            if (string.IsNullOrWhiteSpace(shippingAddress))
                return new AddressEmailModel
                {
                    Street = "Address not specified",
                    City = "Unknown",
                    State = "Unknown",
                    ZipCode = "00000",
                    Country = "Unknown"
                };

            // Simple parsing - you can enhance this based on your address format
            var lines = shippingAddress.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);

            var address = new AddressEmailModel();

            if (lines.Length > 0) address.Street = lines[0].Trim();
            if (lines.Length > 1) address.City = lines[1].Trim();
            if (lines.Length > 2)
            {
                var stateZip = lines[2].Trim();
                var stateZipParts = stateZip.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (stateZipParts.Length >= 2)
                {
                    address.State = stateZipParts[0];
                    address.ZipCode = stateZipParts[1];
                }
                else
                {
                    address.State = stateZip;
                }
            }
            if (lines.Length > 3) address.Country = lines[3].Trim();

            return address;
        }

        private DateTime? CalculateEstimatedDelivery(DateTime orderDate, OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Shipped => orderDate.AddDays(5), // 5 days after shipping
                OrderStatus.Processing or OrderStatus.Paid => orderDate.AddDays(7), // 7 days total
                _ => null
            };
        }

        #endregion

        #region Core Email Sending

        private async Task SendEmailAsync(
            string to,
            string subject,
            string body,
            bool isHtml = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailOptions.FromEmail, _emailOptions.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(to);

                // Add BCC for audit trail if configured
                if (!string.IsNullOrEmpty(_emailOptions.BccEmail))
                {
                    mailMessage.Bcc.Add(_emailOptions.BccEmail);
                }

                // Check if we should actually send emails (development mode)
                if (_emailOptions.IsDevelopmentMode && !_emailOptions.SendEmailsInDevelopment)
                {
                    _logger.LogInformation("Development mode - Email not sent:\nTo: {To}\nSubject: {Subject}", to, subject);
                    return;
                }

                await _smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogDebug("Email sent successfully to {To} with subject: {Subject}", to, subject);
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email to {To}", to);
                throw new ApplicationException($"Failed to send email: {smtpEx.Message}", smtpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}", to);
                throw;
            }
        }

        private void ValidateEmailConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(_emailOptions.SmtpServer))
                errors.Add("SMTP server is not configured");

            if (_emailOptions.SmtpPort <= 0)
                errors.Add("Invalid SMTP port");

            if (string.IsNullOrEmpty(_emailOptions.Username))
                errors.Add("SMTP username is not configured");

            if (string.IsNullOrEmpty(_emailOptions.Password))
                errors.Add("SMTP password is not configured");

            if (string.IsNullOrEmpty(_emailOptions.FromEmail))
                errors.Add("From email is not configured");

            if (string.IsNullOrEmpty(_emailOptions.FromName))
                errors.Add("From name is not configured");

            if (errors.Any())
            {
                throw new InvalidOperationException($"Email configuration errors: {string.Join("; ", errors)}");
            }

            _logger.LogInformation("Email service configured for SMTP server: {SmtpServer}:{SmtpPort}",
                _emailOptions.SmtpServer, _emailOptions.SmtpPort);
        }

        #endregion
    }
}