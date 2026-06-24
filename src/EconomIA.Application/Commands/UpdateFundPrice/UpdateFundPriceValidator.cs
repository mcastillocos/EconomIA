using FluentValidation;

namespace EconomIA.Application.Commands.UpdateFundPrice;

public class UpdateFundPriceValidator : AbstractValidator<UpdateFundPriceCommand>
{
    public UpdateFundPriceValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();
        RuleFor(x => x.NewPrice).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
