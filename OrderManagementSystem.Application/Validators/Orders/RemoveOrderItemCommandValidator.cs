using FluentValidation;
using OrderManagementSystem.Application.Commands.Orders.RemoveOrderItem;

namespace OrderManagementSystem.Application.Validators.Orders
{
    public class RemoveOrderItemCommandValidator : AbstractValidator<RemoveOrderItemCommand>
    {
        public RemoveOrderItemCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Order ID is required");

            RuleFor(x => x.OrderItemId)
                .NotEmpty().WithMessage("Order item ID is required");
        }
    }
}
