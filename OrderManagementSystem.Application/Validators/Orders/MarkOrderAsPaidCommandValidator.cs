using FluentValidation;
using OrderManagementSystem.Application.Commands.Orders.MarkOrderAsPaid;

namespace OrderManagementSystem.Application.Validators.Orders
{
    public class MarkOrderAsPaidCommandValidator : AbstractValidator<MarkOrderAsPaidCommand>
    {
        public MarkOrderAsPaidCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Order ID is required");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("Payment method is required")
                .MaximumLength(50).WithMessage("Payment method cannot exceed 50 characters");

            RuleFor(x => x.AmountPaid)
                .GreaterThan(0).WithMessage("Payment amount must be greater than 0")
                .LessThan(1000000).WithMessage("Payment amount cannot exceed 1,000,000");
        }
    }
}
