using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Debts.Commands;

public record RegisterDebtPaymentCommand(
    Guid DebtId,
    decimal Amount,
    DateTime Date,
    string? Note = null
) : IRequest<Result>;

public class RegisterDebtPaymentHandler : IRequestHandler<RegisterDebtPaymentCommand, Result>
{
    private readonly IRepository<Debt> _debtRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterDebtPaymentHandler(IRepository<Debt> debtRepo, IUnitOfWork unitOfWork)
    {
        _debtRepo = debtRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RegisterDebtPaymentCommand request, CancellationToken ct)
    {
        var debt = await _debtRepo.GetByIdAsync(request.DebtId, ct);
        if (debt == null)
            return Result.Fail("Deuda no encontrada");

        try
        {
            debt.RegisterPayment(request.Amount, request.Date, request.Note);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }

        _debtRepo.Update(debt);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
