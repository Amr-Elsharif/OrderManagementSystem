using FluentValidation;
using OrderManagementSystem.Application.Commands.Orders.CancelOrder;

namespace OrderManagementSystem.Application.Validators.Orders
{
    public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
    {
        public CancelOrderCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Order ID is required");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Cancellation reason is required")
                .MinimumLength(10).WithMessage("Reason must be at least 10 characters")
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
        }
    }
}
