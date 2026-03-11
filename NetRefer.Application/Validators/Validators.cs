using FluentValidation;

namespace NetRefer.Application.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Amount)
            .GreaterThan(0);
    }
}