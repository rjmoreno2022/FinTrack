using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Budgets.Commands;

public record DeleteBudgetCommand(Guid Id) : IRequest<Result>;

public class DeleteBudgetHandler : IRequestHandler<DeleteBudgetCommand, Result>
{
    private readonly IRepository<Budget> _budgetRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBudgetHandler(IRepository<Budget> budgetRepo, IUnitOfWork unitOfWork)
    {
        _budgetRepo = budgetRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteBudgetCommand request, CancellationToken ct)
    {
        var budget = await _budgetRepo.GetByIdAsync(request.Id, ct);
        if (budget == null)
            return Result.Fail("Presupuesto no encontrado");

        _budgetRepo.Delete(budget);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
