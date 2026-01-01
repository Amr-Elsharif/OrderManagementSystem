namespace OrderManagementSystem.Application.DTOs.Products
{
    public class SafeDeleteProductResult
    {
        public bool CanDelete { get; set; }
        public string Message { get; set; }
        public int ActiveOrderCount { get; set; }
        public List<ProductAlternativeDto> Alternatives { get; set; } = new();
        public DateTime? EarliestPossibleDeletion { get; set; }
    }
}
