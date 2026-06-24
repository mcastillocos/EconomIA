using MediatR;

namespace EconomIA.Application.Commands.UpdateFundPrice;

public record UpdateFundPriceCommand(
    Guid FundId,
    decimal NewPrice,
    string Currency) : IRequest<bool>;
