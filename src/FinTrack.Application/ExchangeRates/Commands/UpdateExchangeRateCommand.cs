using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.ExchangeRates.Commands;

public record UpdateExchangeRateCommand(Guid Id, decimal NewRate) : IRequest<Result>;

public class UpdateExchangeRateHandler : IRequestHandler<UpdateExchangeRateCommand, Result>
{
    private readonly IRepository<ExchangeRate> _rateRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateExchangeRateHandler(IRepository<ExchangeRate> rateRepo, IUnitOfWork unitOfWork)
    {
        _rateRepo = rateRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateExchangeRateCommand request, CancellationToken ct)
    {
        if (request.NewRate <= 0)
            return Result.Fail("La tasa de cambio debe ser mayor a cero");

        var rate = await _rateRepo.GetByIdAsync(request.Id, ct);
        if (rate == null)
            return Result.Fail("Tasa de cambio no encontrada");

        rate.UpdateRate(request.NewRate);
        _rateRepo.Update(rate);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
