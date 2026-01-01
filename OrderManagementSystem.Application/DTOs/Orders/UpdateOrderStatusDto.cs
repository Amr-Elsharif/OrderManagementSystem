using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.DTOs.Orders
{
    public class UpdateOrderStatusDto
    {
        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
