using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Budgets.Commands;

public record CreateBudgetCommand(
    string Name,
    decimal LimitAmount,
    string Currency,
    string Period,
    DateTime StartDate,
    DateTime EndDate,
    Guid? CategoryId = null
) : IRequest<Result<Guid>>;

public class CreateBudgetHandler : IRequestHandler<CreateBudgetCommand, Result<Guid>>
{
    private readonly IRepository<Budget> _budgetRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBudgetHandler(IRepository<Budget> budgetRepo, IUnitOfWork unitOfWork)
    {
        _budgetRepo = budgetRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateBudgetCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
            return Result<Guid>.Fail($"Moneda inválida: {request.Currency}");

        if (!Enum.TryParse<BudgetPeriod>(request.Period, true, out var period))
            return Result<Guid>.Fail($"Período de presupuesto inválido: {request.Period}");

        if (request.StartDate >= request.EndDate)
            return Result<Guid>.Fail("La fecha de inicio debe ser anterior a la fecha de fin");

        var budget = new Budget(
            request.Name,
            request.LimitAmount,
            currency,
            period,
            request.StartDate,
            request.EndDate,
            request.CategoryId
        );

        await _budgetRepo.AddAsync(budget, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Ok(budget.Id);
    }
}
