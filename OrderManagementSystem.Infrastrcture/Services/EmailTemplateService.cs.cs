using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagementSystem.Application.Interfaces;
using OrderManagementSystem.Application.Models.Emails;
using System.Text;
using System.Text.Encodings.Web;

namespace OrderManagementSystem.Infrastructure.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(
            IHostEnvironment environment,
            ILogger<EmailTemplateService> logger)
        {
            _environment = environment;
            _logger = logger;
            EnsureTemplateDirectoryExists();
        }

        public async Task<string> GenerateOrderConfirmationEmailAsync(OrderConfirmationEmail model)
        {
            var template = await LoadTemplateAsync("OrderConfirmation.html");

            return template
                .Replace("{{OrderId}}", model.OrderId?.ToString("N") ?? "N/A")
                .Replace("{{CustomerName}}", HtmlEncoder.Default.Encode(model.CustomerName))
                .Replace("{{OrderDate}}", model.OrderDate.ToString("f"))
                .Replace("{{TotalAmount}}", model.TotalAmount.ToString("C"))
                .Replace("{{ShippingAddress}}", FormatAddress(model.ShippingAddress))
                .Replace("{{OrderItems}}", GenerateOrderItemsHtml(model.Items))
                .Replace("{{TrackingNumber}}", model.TrackingNumber ?? "Will be provided soon")
                .Replace("{{EstimatedDelivery}}", model.EstimatedDelivery?.ToString("D") ?? "5-7 business days");
        }

        public async Task<string> GeneratePaymentConfirmationEmailAsync(PaymentConfirmationEmail model)
        {
            var template = await LoadTemplateAsync("PaymentConfirmation.html");

            return template
                .Replace("{{OrderId}}", model.OrderId?.ToString("N") ?? "N/A")
                .Replace("{{CustomerName}}", HtmlEncoder.Default.Encode(model.CustomerName))
                .Replace("{{PaymentDate}}", model.PaymentDate.ToString("f"))
                .Replace("{{Amount}}", model.Amount.ToString("C"))
                .Replace("{{PaymentMethod}}", model.PaymentMethod)
                .Replace("{{TransactionId}}", model.TransactionId);
        }

        public async Task<string> GenerateShippingNotificationEmailAsync(ShippingNotificationEmail model)
        {
            var template = await LoadTemplateAsync("ShippingNotification.html");

            return template
                .Replace("{{OrderId}}", model.OrderId?.ToString("N") ?? "N/A")
                .Replace("{{CustomerName}}", HtmlEncoder.Default.Encode(model.CustomerName))
                .Replace("{{TrackingNumber}}", model.TrackingNumber)
                .Replace("{{ShippingCarrier}}", model.ShippingCarrier)
                .Replace("{{ShippingAddress}}", FormatAddress(model.ShippingAddress))
                .Replace("{{EstimatedDelivery}}", model.EstimatedDelivery?.ToString("D") ?? "5-7 business days");
        }

        public async Task<string> GenerateCancellationNotificationEmailAsync(CancellationNotificationEmail model)
        {
            var template = await LoadTemplateAsync("CancellationNotification.html");

            return template
                .Replace("{{OrderId}}", model.OrderId?.ToString("N") ?? "N/A")
                .Replace("{{CustomerName}}", HtmlEncoder.Default.Encode(model.CustomerName))
                .Replace("{{CancellationDate}}", model.CancellationDate.ToString("f"))
                .Replace("{{Reason}}", HtmlEncoder.Default.Encode(model.Reason))
                .Replace("{{RefundAmount}}", model.RefundAmount.ToString("C"))
                .Replace("{{RefundReference}}", model.RefundReference ?? "N/A");
        }

        public async Task<string> GenerateOrderStatusUpdateEmailAsync(OrderStatusUpdateEmail model)
        {
            var template = await LoadTemplateAsync("OrderStatusUpdate.html");

            return template
                .Replace("{{OrderId}}", model.OrderId?.ToString("N") ?? "N/A")
                .Replace("{{CustomerName}}", HtmlEncoder.Default.Encode(model.CustomerName))
                .Replace("{{OldStatus}}", model.OldStatus)
                .Replace("{{NewStatus}}", model.NewStatus)
                .Replace("{{StatusChangeDate}}", model.StatusChangeDate.ToString("f"))
                .Replace("{{Notes}}", HtmlEncoder.Default.Encode(model.Notes));
        }

        public async Task<string> GenerateStockOutAlertEmailAsync(StockOutAlertEmail model)
        {
            var template = await LoadTemplateAsync("StockOutAlert.html");

            return template
                .Replace("{{ProductId}}", model.ProductId.ToString("N"))
                .Replace("{{ProductName}}", HtmlEncoder.Default.Encode(model.ProductName))
                .Replace("{{Sku}}", model.Sku)
                .Replace("{{Category}}", model.Category)
                .Replace("{{Price}}", model.Price.ToString("C"))
                .Replace("{{LastStockCount}}", model.LastStockCount.ToString())
                .Replace("{{LastRestockDate}}", model.LastRestockDate.ToString("f"))
                .Replace("{{SupplierName}}", model.SupplierName ?? "N/A");
        }

        public async Task<string> GenerateProductDeletionEmailAsync(ProductDeletionEmail model)
        {
            var template = await LoadTemplateAsync("ProductDeletion.html");

            return template
                .Replace("{{ProductId}}", model.ProductId.ToString("N"))
                .Replace("{{ProductName}}", HtmlEncoder.Default.Encode(model.ProductName))
                .Replace("{{Sku}}", model.Sku)
                .Replace("{{Reason}}", HtmlEncoder.Default.Encode(model.Reason))
                .Replace("{{DeletedBy}}", model.DeletedBy)
                .Replace("{{DeletionDate}}", model.DeletionDate.ToString("f"))
                .Replace("{{AffectedOrders}}", string.Join("<br>", model.AffectedOrders));
        }

        public async Task<string> GenerateHighValueProductAlertEmailAsync(HighValueProductEmail model)
        {
            var template = await LoadTemplateAsync("HighValueProductAlert.html");

            return template
                .Replace("{{ProductId}}", model.ProductId.ToString("N"))
                .Replace("{{ProductName}}", HtmlEncoder.Default.Encode(model.ProductName))
                .Replace("{{Sku}}", model.Sku)
                .Replace("{{Price}}", model.Price.ToString("C"))
                .Replace("{{CostPrice}}", (model.CostPrice ?? 0).ToString("C"))
                .Replace("{{ProfitMargin}}", $"{model.ProfitMargin:P1}")
                .Replace("{{CurrentStock}}", model.CurrentStock.ToString())
                .Replace("{{MonthlySales}}", model.MonthlySales.ToString())
                .Replace("{{MonthlyRevenue}}", model.MonthlyRevenue.ToString("C"));
        }


        #region Private Helper Methods

        private async Task<string> LoadTemplateAsync(string templateName)
        {
            var templatePath = Path.Combine(_environment.ContentRootPath, "EmailTemplates", templateName);

            if (File.Exists(templatePath))
            {
                return await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
            }

            _logger.LogWarning("Template {TemplateName} not found, using fallback", templateName);
            return await GetFallbackTemplateAsync(templateName);
        }

        private void EnsureTemplateDirectoryExists()
        {
            var templateDir = Path.Combine(_environment.ContentRootPath, "EmailTemplates");
            if (!Directory.Exists(templateDir))
            {
                Directory.CreateDirectory(templateDir);
                CreateDefaultTemplates(templateDir);
            }
        }

        private async Task<string> GetFallbackTemplateAsync(string templateType)
        {
            return templateType switch
            {
                "OrderConfirmation.html" => await CreateOrderConfirmationFallback(),
                "PaymentConfirmation.html" => await CreatePaymentConfirmationFallback(),
                "ShippingNotification.html" => await CreateShippingNotificationFallback(),
                _ => await CreateBasicEmailFallback()
            };
        }

        private string FormatAddress(AddressEmailModel address)
        {
            return $"{address.Street}<br>{address.City}, {address.State} {address.ZipCode}<br>{address.Country}";
        }

        private string GenerateOrderItemsHtml(List<OrderItemEmailModel> items)
        {
            if (!items.Any()) return "<tr><td colspan='4'>No items</td></tr>";

            var sb = new StringBuilder();
            foreach (var item in items)
            {
                sb.AppendLine($@"
                <tr>
                    <td>{HtmlEncoder.Default.Encode(item.ProductName)}<br><small>{item.Sku}</small></td>
                    <td style='text-align: center;'>{item.Quantity}</td>
                    <td style='text-align: right;'>{item.UnitPrice:C}</td>
                    <td style='text-align: right;'><strong>{item.TotalPrice:C}</strong></td>
                </tr>");
            }
            return sb.ToString();
        }

        #endregion

        #region Fallback Templates

        private async Task<string> CreateOrderConfirmationFallback()
        {
            return @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Order Confirmation</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; }
                    .container { max-width: 600px; margin: 0 auto; background: #fff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                    .header { background: #4CAF50; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }
                    .content { padding: 40px; }
                    .footer { text-align: center; padding: 20px; color: #777; font-size: 12px; border-top: 1px solid #eee; }
                    table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                    th { background: #f5f5f5; padding: 12px; text-align: left; border-bottom: 2px solid #ddd; }
                    td { padding: 12px; border-bottom: 1px solid #eee; }
                    .total { font-size: 18px; font-weight: bold; color: #4CAF50; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Order Confirmation</h1>
                    </div>
                    <div class='content'>
                        <p>Dear {{CustomerName}},</p>
                        <p>Thank you for your order! We've received your order and it's being processed.</p>
                        
                        <h3>Order #{{OrderId}}</h3>
                        <p><strong>Order Date:</strong> {{OrderDate}}</p>
                        <p><strong>Status:</strong> Processing</p>
                        
                        <h4>Order Items</h4>
                        <table>
                            <thead>
                                <tr>
                                    <th>Product</th>
                                    <th>Quantity</th>
                                    <th>Price</th>
                                    <th>Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                {{OrderItems}}
                            </tbody>
                        </table>
                        
                        <div style='text-align: right;'>
                            <p class='total'>Total Amount: {{TotalAmount}}</p>
                        </div>
                        
                        <h4>Shipping Address</h4>
                        <p>{{ShippingAddress}}</p>
                        
                        <p><strong>Tracking Number:</strong> {{TrackingNumber}}</p>
                        <p><strong>Estimated Delivery:</strong> {{EstimatedDelivery}}</p>
                        
                        <p>You can track your order status anytime by visiting our website.</p>
                        
                        <p>Thank you for shopping with us!</p>
                        <p><strong>The Order Management Team</strong></p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated email. Please do not reply to this message.</p>
                        <p>&copy; 2024 Order Management System. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private async Task<string> CreatePaymentConfirmationFallback()
        {
            return @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Payment Confirmation</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .success { color: #4CAF50; font-weight: bold; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Payment Confirmation</h2>
                    <p>Dear {{CustomerName}},</p>
                    <p class='success'>We have successfully received your payment!</p>
                    
                    <h3>Payment Details</h3>
                    <p><strong>Order #:</strong> {{OrderId}}</p>
                    <p><strong>Payment Date:</strong> {{PaymentDate}}</p>
                    <p><strong>Amount:</strong> {{Amount}}</p>
                    <p><strong>Payment Method:</strong> {{PaymentMethod}}</p>
                    <p><strong>Transaction ID:</strong> {{TransactionId}}</p>
                    
                    <p>Thank you for your payment. Your order is now being processed.</p>
                </div>
            </body>
            </html>";
        }

        private async Task<string> CreateShippingNotificationFallback()
        {
            return @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Your Order Has Shipped!</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .tracking { background: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>🎉 Your Order Has Shipped!</h2>
                    <p>Dear {{CustomerName}},</p>
                    <p>Great news! Your order #{{OrderId}} has been shipped and is on its way to you.</p>
                    
                    <div class='tracking'>
                        <h3>Tracking Information</h3>
                        <p><strong>Tracking Number:</strong> {{TrackingNumber}}</p>
                        <p><strong>Shipping Carrier:</strong> {{ShippingCarrier}}</p>
                        <p><strong>Estimated Delivery:</strong> {{EstimatedDelivery}}</p>
                    </div>
                    
                    <h3>Shipping Address</h3>
                    <p>{{ShippingAddress}}</p>
                    
                    <p>You can track your package using the tracking number above.</p>
                    <p>Thank you for your order!</p>
                </div>
            </body>
            </html>";
        }

        private async Task<string> CreateBasicEmailFallback()
        {
            return @"
            <!DOCTYPE html>
            <html>
            <body>
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2>{{Subject}}</h2>
                    <p>This is an automated notification from Order Management System.</p>
                    <p>Timestamp: {{Timestamp}}</p>
                </div>
            </body>
            </html>";
        }

        #endregion

        #region Default Template Creation

        private void CreateDefaultTemplates(string templateDir)
        {
            // Create default templates
            var defaultTemplates = new Dictionary<string, string>
            {
                ["OrderConfirmation.html"] = CreateOrderConfirmationFallback().Result,
                ["PaymentConfirmation.html"] = CreatePaymentConfirmationFallback().Result,
                ["ShippingNotification.html"] = CreateShippingNotificationFallback().Result,
                ["CancellationNotification.html"] = CreateBasicEmailFallback().Result,
                ["OrderStatusUpdate.html"] = CreateBasicEmailFallback().Result,
                ["StockOutAlert.html"] = CreateBasicEmailFallback().Result,
                ["ProductDeletion.html"] = CreateBasicEmailFallback().Result,
                ["HighValueProductAlert.html"] = CreateBasicEmailFallback().Result
            };

            foreach (var template in defaultTemplates)
            {
                File.WriteAllText(Path.Combine(templateDir, template.Key), template.Value);
            }

            _logger.LogInformation("Created default email templates in {TemplateDir}", templateDir);
        }

        #endregion
    }
}