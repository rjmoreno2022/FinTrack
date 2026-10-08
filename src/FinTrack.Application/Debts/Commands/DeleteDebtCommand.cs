using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Debts.Commands;

public record DeleteDebtCommand(Guid Id) : IRequest<Result>;

public class DeleteDebtHandler : IRequestHandler<DeleteDebtCommand, Result>
{
    private readonly IRepository<Debt> _debtRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDebtHandler(IRepository<Debt> debtRepo, IUnitOfWork unitOfWork)
    {
        _debtRepo = debtRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteDebtCommand request, CancellationToken ct)
    {
        var debt = await _debtRepo.GetByIdAsync(request.Id, ct);
        if (debt == null)
            return Result.Fail("Deuda no encontrada");

        _debtRepo.Delete(debt);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
