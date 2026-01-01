namespace OrderManagementSystem.Application.DTOs.Customers
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Phone { get; private set; } = string.Empty;
        public string Address { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
    }
}
