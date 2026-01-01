using FluentValidation;
using OrderManagementSystem.Application.Commands.Orders.UpdateOrder;
using OrderManagementSystem.Domain.Enums;

namespace OrderManagementSystem.Application.Validators.Orders
{
    public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
    {
        public UpdateOrderStatusCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Order ID is required");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid order status");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");

            // Business rule validation based on status
            When(x => x.Status == OrderStatus.Cancelled, () =>
            {
                RuleFor(x => x.Notes)
                    .NotEmpty().WithMessage("Cancellation reason is required")
                    .MinimumLength(10).WithMessage("Cancellation reason must be at least 10 characters");
            });
        }
    }
}
