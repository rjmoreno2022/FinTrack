using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Models;
using FinTrack.Application.ExchangeRates.DTOs;
using MediatR;

namespace FinTrack.Application.ExchangeRates.Queries;

public record GetLiveOfficialRatesQuery : IRequest<Result<OfficialRatesDto>>;

public class GetLiveOfficialRatesQueryHandler : IRequestHandler<GetLiveOfficialRatesQuery, Result<OfficialRatesDto>>
{
    private readonly IBcvRateProvider _bcvRateProvider;

    public GetLiveOfficialRatesQueryHandler(IBcvRateProvider bcvRateProvider)
    {
        _bcvRateProvider = bcvRateProvider;
    }

    public async Task<Result<OfficialRatesDto>> Handle(GetLiveOfficialRatesQuery request, CancellationToken cancellationToken)
    {
        var result = await _bcvRateProvider.GetLiveOfficialRatesAsync(cancellationToken);
        if (result == null || result.UsdRate <= 0 || result.EurRate <= 0)
        {
            return Result<OfficialRatesDto>.Fail("No fue posible obtener las cotizaciones oficiales del BCV en este momento.");
        }

        return Result<OfficialRatesDto>.Ok(result);
    }
}
