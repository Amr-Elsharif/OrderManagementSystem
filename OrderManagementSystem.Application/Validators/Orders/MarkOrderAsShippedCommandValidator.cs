using FluentValidation;
using OrderManagementSystem.Application.Commands.Orders.MarkOrderAsShipped;

namespace OrderManagementSystem.Application.Validators.Orders
{
    public class MarkOrderAsShippedCommandValidator : AbstractValidator<MarkOrderAsShippedCommand>
    {
        public MarkOrderAsShippedCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Order ID is required");

            RuleFor(x => x.TrackingNumber)
                .NotEmpty().WithMessage("Tracking number is required")
                .MaximumLength(100).WithMessage("Tracking number cannot exceed 100 characters");
        }
    }
}
