using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Debts.Commands;

public record UpdateDebtCommand(
    Guid Id,
    string Creditor,
    DateTime? DueDate = null,
    string? Description = null,
    decimal InterestRate = 0
) : IRequest<Result>;

public class UpdateDebtHandler : IRequestHandler<UpdateDebtCommand, Result>
{
    private readonly IRepository<Debt> _debtRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDebtHandler(IRepository<Debt> debtRepo, IUnitOfWork unitOfWork)
    {
        _debtRepo = debtRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateDebtCommand request, CancellationToken ct)
    {
        var debt = await _debtRepo.GetByIdAsync(request.Id, ct);
        if (debt == null)
            return Result.Fail($"Deuda con ID {request.Id} no encontrada");

        try
        {
            debt.Update(request.Creditor, request.Description, request.DueDate, request.InterestRate);
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
