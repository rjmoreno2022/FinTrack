using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Common.Models;
using FinTrack.Application.ExchangeRates.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.ExchangeRates.Queries;

public record GetExchangeRatesListQuery(bool ActiveOnly = false) : IRequest<Result<List<ExchangeRateDto>>>;

public class GetExchangeRatesListHandler : IRequestHandler<GetExchangeRatesListQuery, Result<List<ExchangeRateDto>>>
{
    private readonly IRepository<ExchangeRate> _rateRepo;
    private readonly IMapper _mapper;

    public GetExchangeRatesListHandler(IRepository<ExchangeRate> rateRepo, IMapper mapper)
    {
        _rateRepo = rateRepo;
        _mapper = mapper;
    }

    public async Task<Result<List<ExchangeRateDto>>> Handle(GetExchangeRatesListQuery request, CancellationToken ct)
    {
        var rates = await _rateRepo.ListAllAsync(ct);

        if (request.ActiveOnly)
        {
            rates = rates.Where(r => r.IsActive).ToList();
        }

        var dtos = _mapper.Map<List<ExchangeRateDto>>(rates.OrderByDescending(r => r.Date));
        return Result<List<ExchangeRateDto>>.Ok(dtos);
    }
}
