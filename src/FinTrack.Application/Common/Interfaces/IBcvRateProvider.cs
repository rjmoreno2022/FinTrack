using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.ExchangeRates.DTOs;

namespace FinTrack.Application.Common.Interfaces;

public interface IBcvRateProvider
{
    Task<BcvRateResultDto?> GetLiveRateAsync(CancellationToken cancellationToken = default);
    Task<OfficialRatesDto?> GetLiveOfficialRatesAsync(CancellationToken cancellationToken = default);
}
