using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Budgets.Commands;

public record UpdateBudgetCommand(
    Guid Id,
    string Name,
    decimal LimitAmount,
    DateTime StartDate,
    DateTime EndDate,
    Guid? CategoryId = null
) : IRequest<Result>;

public class UpdateBudgetHandler : IRequestHandler<UpdateBudgetCommand, Result>
{
    private readonly IRepository<Budget> _budgetRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBudgetHandler(IRepository<Budget> budgetRepo, IUnitOfWork unitOfWork)
    {
        _budgetRepo = budgetRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateBudgetCommand request, CancellationToken ct)
    {
        var budget = await _budgetRepo.GetByIdAsync(request.Id, ct);
        if (budget == null)
            return Result.Fail($"Presupuesto con ID {request.Id} no encontrado");

        try
        {
            budget.Update(request.Name, request.LimitAmount, request.CategoryId, request.StartDate, request.EndDate);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }

        _budgetRepo.Update(budget);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
