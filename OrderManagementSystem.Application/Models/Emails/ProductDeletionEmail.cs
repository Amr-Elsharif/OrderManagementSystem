namespace OrderManagementSystem.Application.Models.Emails
{
    public class ProductDeletionEmail : BaseEmailModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string DeletedBy { get; set; } = string.Empty;
        public DateTime DeletionDate { get; set; }
        public List<string> AffectedOrders { get; set; } = new();
    }
}
