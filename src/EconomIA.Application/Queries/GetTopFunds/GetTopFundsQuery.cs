using EconomIA.Application.DTOs;
using MediatR;

namespace EconomIA.Application.Queries.GetTopFunds;

public record GetTopFundsQuery(int Count = 100) : IRequest<IReadOnlyList<FundDto>>;
