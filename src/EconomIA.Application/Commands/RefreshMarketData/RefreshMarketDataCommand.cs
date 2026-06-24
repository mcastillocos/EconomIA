using MediatR;

namespace EconomIA.Application.Commands.RefreshMarketData;

public record RefreshMarketDataCommand(int TopCount = 100) : IRequest<int>;
