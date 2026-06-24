using EconomIA.Application.DTOs;
using EconomIA.Domain.ValueObjects;
using MediatR;

namespace EconomIA.Application.Queries.GetFundsByRisk;

public record GetFundsByRiskQuery(RiskLevel RiskLevel) : IRequest<IReadOnlyList<FundDto>>;
