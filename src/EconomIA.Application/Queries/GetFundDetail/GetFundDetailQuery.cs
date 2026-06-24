using EconomIA.Application.DTOs;
using MediatR;

namespace EconomIA.Application.Queries.GetFundDetail;

public record GetFundDetailQuery(Guid FundId) : IRequest<FundDto?>;
