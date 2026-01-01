using FluentValidation;
using OrderManagementSystem.Application.Commands.Orders.CreateOrder;

namespace OrderManagementSystem.Application.Validators.Orders
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("Customer ID is required");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Order must have at least one item");

            RuleForEach(x => x.Items)
                .SetValidator(new CreateOrderItemCommandValidator());

            RuleFor(x => x.ShippingAddress)
                .MaximumLength(500).WithMessage("Shipping address cannot exceed 500 characters");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
        }
    }
}