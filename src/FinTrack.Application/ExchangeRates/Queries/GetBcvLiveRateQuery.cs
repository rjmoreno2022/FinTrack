using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Models;
using FinTrack.Application.ExchangeRates.DTOs;
using MediatR;

namespace FinTrack.Application.ExchangeRates.Queries;

public record GetBcvLiveRateQuery : IRequest<Result<BcvRateResultDto>>;

public class GetBcvLiveRateQueryHandler : IRequestHandler<GetBcvLiveRateQuery, Result<BcvRateResultDto>>
{
    private readonly IBcvRateProvider _bcvRateProvider;

    public GetBcvLiveRateQueryHandler(IBcvRateProvider bcvRateProvider)
    {
        _bcvRateProvider = bcvRateProvider;
    }

    public async Task<Result<BcvRateResultDto>> Handle(GetBcvLiveRateQuery request, CancellationToken cancellationToken)
    {
        var result = await _bcvRateProvider.GetLiveRateAsync(cancellationToken);
        if (result == null || result.Rate <= 0)
        {
            return Result<BcvRateResultDto>.Fail("No fue posible obtener la cotización oficial del BCV en este momento.");
        }

        return Result<BcvRateResultDto>.Ok(result);
    }
}
