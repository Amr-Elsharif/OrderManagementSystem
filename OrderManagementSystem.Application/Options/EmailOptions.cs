namespace OrderManagementSystem.Application.Options
{
    public class EmailOptions
    {
        public const string SectionName = "Email";

        // SMTP Configuration
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int TimeoutMilliseconds { get; set; } = 30000;

        // Email Details
        public string FromEmail { get; set; } = "noreply@ordermanagement.com";
        public string FromName { get; set; } = "Order Management System";

        // Audit/Logging
        public string BccEmail { get; set; } = string.Empty;

        // Alert Recipients
        public List<string> AdminEmails { get; set; } = new();
        public List<string> FinanceEmails { get; set; } = new();

        // Development Settings
        public bool IsDevelopmentMode { get; set; } = true;
        public bool SendEmailsInDevelopment { get; set; } = false;

        // Retry Policy
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 1000;

        // Templates
        public string TemplateDirectory { get; set; } = "EmailTemplates";
        public bool UseInMemoryTemplates { get; set; } = false;
    }
}