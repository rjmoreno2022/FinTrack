using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Budgets.DTOs;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Budgets.Queries;

public record GetBudgetsListQuery(bool ActiveOnly = false) : IRequest<Result<List<BudgetDto>>>;

public class GetBudgetsListHandler : IRequestHandler<GetBudgetsListQuery, Result<List<BudgetDto>>>
{
    private readonly IRepository<Budget> _budgetRepo;
    private readonly IRepository<Account> _accountRepo;
    private readonly IMapper _mapper;

    public GetBudgetsListHandler(
        IRepository<Budget> budgetRepo,
        IRepository<Account> accountRepo,
        IMapper mapper)
    {
        _budgetRepo = budgetRepo;
        _accountRepo = accountRepo;
        _mapper = mapper;
    }

    public async Task<Result<List<BudgetDto>>> Handle(GetBudgetsListQuery request, CancellationToken ct)
    {
        var budgets = await _budgetRepo.ListAllAsync(ct);
        if (request.ActiveOnly)
        {
            budgets = budgets.Where(b => b.IsActive).ToList();
        }

        var accounts = await _accountRepo.ListAllAsync(ct);
        var allExpenses = accounts
            .SelectMany(a => a.Transactions)
            .Where(t => t.Type == TransactionType.Expense)
            .ToList();

        var result = new List<BudgetDto>();

        foreach (var budget in budgets.OrderByDescending(b => b.CreatedAt))
        {
            var matchingExpenses = allExpenses
                .Where(t => t.Date >= budget.StartDate && t.Date <= budget.EndDate);

            if (budget.CategoryId.HasValue)
            {
                matchingExpenses = matchingExpenses.Where(t => t.CategoryId == budget.CategoryId.Value);
            }

            var calculatedSpent = matchingExpenses.Sum(t => t.Amount);
            var actualSpent = Math.Max(budget.SpentAmount, calculatedSpent);

            var dto = _mapper.Map<BudgetDto>(budget);
            dto.SpentAmount = actualSpent;
            dto.Remaining = dto.LimitAmount - actualSpent;
            dto.UsagePercentage = dto.LimitAmount > 0 ? (actualSpent / dto.LimitAmount) * 100 : 0;
            dto.IsOverBudget = actualSpent > dto.LimitAmount;

            result.Add(dto);
        }

        return Result<List<BudgetDto>>.Ok(result);
    }
}
