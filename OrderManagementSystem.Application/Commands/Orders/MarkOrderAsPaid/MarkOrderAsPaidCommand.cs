using MediatR;

namespace OrderManagementSystem.Application.Commands.Orders.MarkOrderAsPaid
{
    public class MarkOrderAsPaidCommand : IRequest<bool>
    {
        public Guid OrderId { get; init; }
        public string PaymentMethod { get; init; } = string.Empty;
        public decimal AmountPaid { get; init; }
        public string? PaidBy { get; init; }
    }
}
