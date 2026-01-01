using FluentValidation;
using OrderManagementSystem.Application.Commands.Products.UpdateProductStock;

namespace OrderManagementSystem.Application.Validators.Products
{
    public class UpdateProductStockCommandValidator : AbstractValidator<UpdateProductStockCommand>
    {
        public UpdateProductStockCommandValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Product ID is required");

            RuleFor(x => x.QuantityChange)
                .NotEqual(0).WithMessage("Quantity change cannot be 0")
                .LessThanOrEqualTo(1000).WithMessage("Cannot change stock by more than 1000 units at once")
                .GreaterThanOrEqualTo(-1000).WithMessage("Cannot reduce stock by more than 1000 units at once");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
        }
    }
}
