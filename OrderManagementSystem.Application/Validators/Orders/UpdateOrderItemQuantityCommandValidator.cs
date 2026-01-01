using FluentValidation;
using OrderManagementSystem.Application.Commands.Orders.UpdateOrderItemQuantity;

namespace OrderManagementSystem.Application.Validators.Orders
{
    public class UpdateOrderItemQuantityCommandValidator : AbstractValidator<UpdateOrderItemQuantityCommand>
    {
        public UpdateOrderItemQuantityCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Order ID is required");

            RuleFor(x => x.OrderItemId)
                .NotEmpty().WithMessage("Order item ID is required");

            RuleFor(x => x.NewQuantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Quantity cannot exceed 100");
        }
    }
}
