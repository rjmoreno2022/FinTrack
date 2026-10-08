using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Accounts.Queries;
using FinTrack.Application.Budgets.Queries;
using FinTrack.Application.Debts.Queries;
using FinTrack.Application.Goals.Queries;
using FinTrack.Application.Transactions.Queries;
using FinTrack.Domain.Enums;
using MediatR;

namespace FinTrack.Web.Services;

public class DashboardSummaryDto
{
    public decimal TotalBalanceUSD { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public int ActiveAccountsCount { get; set; }
    public int TransactionsCount { get; set; }
    public decimal SavingsRate =>
        MonthlyIncome > 0 ? Math.Max(0, ((MonthlyIncome - MonthlyExpenses) / MonthlyIncome) * 100) : 0;
}

public class CategoryExpenseReportDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public string? Color { get; set; }
}

public class MonthlyCashFlowDto
{
    public string MonthLabel { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal NetSavings => Income - Expense;
}

public class FinancialHealthSummaryDto
{
    public decimal SavingsRate { get; set; }
    public int ActiveBudgetsCount { get; set; }
    public int OverbudgetCount { get; set; }
    public decimal TotalDebtOwed { get; set; }
    public decimal TotalDebtReceivable { get; set; }
    public int TotalGoalsActive { get; set; }
    public decimal GoalsProgressAverage { get; set; }
}

public interface IReportService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<List<CategoryExpenseReportDto>> GetCategoryExpensesAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<MonthlyCashFlowDto>> GetMonthlyCashFlowAsync(int monthsCount = 6);
    Task<FinancialHealthSummaryDto> GetFinancialHealthSummaryAsync();
}

public class ReportService : IReportService
{
    private readonly IMediator _mediator;

    public ReportService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var accountsResult = await _mediator.Send(new GetAccountsListQuery(PageNumber: 1, PageSize: 100));
        var accounts = accountsResult.IsSuccess && accountsResult.Value != null
            ? accountsResult.Value.Items
            : new List<AccountDto>();

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

        var transactionsResult = await _mediator.Send(new GetTransactionsListQuery(
            StartDate: startOfMonth, EndDate: endOfMonth, PageNumber: 1, PageSize: 5000));

        var transactions = transactionsResult.IsSuccess && transactionsResult.Value != null
            ? transactionsResult.Value.Items
            : new List<Application.Transactions.DTOs.TransactionDto>();

        var totalBalance = accounts.Sum(a => a.Balance);
        var monthlyIncome = transactions
            .Where(t => t.Type == nameof(TransactionType.Income))
            .Sum(t => t.Amount);
        var monthlyExpenses = transactions
            .Where(t => t.Type == nameof(TransactionType.Expense))
            .Sum(t => t.Amount);

        return new DashboardSummaryDto
        {
            TotalBalanceUSD = totalBalance,
            MonthlyIncome = monthlyIncome,
            MonthlyExpenses = monthlyExpenses,
            ActiveAccountsCount = accounts.Count,
            TransactionsCount = transactions.Count
        };
    }

    public async Task<List<CategoryExpenseReportDto>> GetCategoryExpensesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        var start = startDate ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = endDate ?? start.AddMonths(1).AddTicks(-1);

        var transactionsResult = await _mediator.Send(new GetTransactionsListQuery(
            StartDate: start, EndDate: end, PageNumber: 1, PageSize: 5000));

        var transactions = transactionsResult.IsSuccess && transactionsResult.Value != null
            ? transactionsResult.Value.Items.Where(t => t.Type == nameof(TransactionType.Expense)).ToList()
            : new List<Application.Transactions.DTOs.TransactionDto>();

        var totalExpenses = transactions.Sum(t => t.Amount);
        if (totalExpenses == 0)
            return new List<CategoryExpenseReportDto>();

        var grouped = transactions
            .GroupBy(t => string.IsNullOrWhiteSpace(t.CategoryName) ? "General / Sin Categoría" : t.CategoryName)
            .Select(g =>
            {
                var categorySum = g.Sum(t => t.Amount);
                return new CategoryExpenseReportDto
                {
                    CategoryName = g.Key,
                    Amount = categorySum,
                    Percentage = Math.Round((categorySum / totalExpenses) * 100, 1)
                };
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        return grouped;
    }

    public async Task<List<MonthlyCashFlowDto>> GetMonthlyCashFlowAsync(int monthsCount = 6)
    {
        var now = DateTime.UtcNow;
        var startMonth = now.AddMonths(-monthsCount + 1);
        var startDate = new DateTime(startMonth.Year, startMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddTicks(-1);

        var transactionsResult = await _mediator.Send(new GetTransactionsListQuery(
            StartDate: startDate, EndDate: endDate, PageNumber: 1, PageSize: 10000));

        var transactions = transactionsResult.IsSuccess && transactionsResult.Value != null
            ? transactionsResult.Value.Items.ToList()
            : new List<Application.Transactions.DTOs.TransactionDto>();

        var result = new List<MonthlyCashFlowDto>();
        var spanishCulture = new CultureInfo("es-ES");

        for (int i = 0; i < monthsCount; i++)
        {
            var dateCursor = startDate.AddMonths(i);
            var mStart = new DateTime(dateCursor.Year, dateCursor.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            var monthTrans = transactions.Where(t => t.Date >= mStart && t.Date <= mEnd).ToList();

            var income = monthTrans.Where(t => t.Type == nameof(TransactionType.Income)).Sum(t => t.Amount);
            var expense = monthTrans.Where(t => t.Type == nameof(TransactionType.Expense)).Sum(t => t.Amount);

            var monthName = mStart.ToString("MMM yy", spanishCulture);
            // Capitalizar primera letra (ej. "Ene 26")
            monthName = char.ToUpper(monthName[0]) + monthName[1..];

            result.Add(new MonthlyCashFlowDto
            {
                MonthLabel = monthName,
                Year = mStart.Year,
                Month = mStart.Month,
                Income = income,
                Expense = expense
            });
        }

        return result;
    }

    public async Task<FinancialHealthSummaryDto> GetFinancialHealthSummaryAsync()
    {
        var summary = await GetDashboardSummaryAsync();

        var budgetsResult = await _mediator.Send(new GetBudgetsListQuery(ActiveOnly: true));
        var budgets = budgetsResult.IsSuccess && budgetsResult.Value != null
            ? budgetsResult.Value
            : new List<FinTrack.Application.Budgets.DTOs.BudgetDto>();

        var debtsResult = await _mediator.Send(new GetDebtsListQuery());
        var debts = debtsResult.IsSuccess && debtsResult.Value != null
            ? debtsResult.Value
            : new List<FinTrack.Application.Debts.DTOs.DebtDto>();

        var goalsResult = await _mediator.Send(new GetGoalsListQuery(ActiveOnly: true));
        var goals = goalsResult.IsSuccess && goalsResult.Value != null
            ? goalsResult.Value
            : new List<FinTrack.Application.Goals.DTOs.GoalDto>();

        var totalOwed = debts
            .Where(d => d.Type == "Owed" && d.Status == "Active")
            .Sum(d => d.RemainingAmount);

        var totalReceivable = debts
            .Where(d => d.Type == "Receivable" && d.Status == "Active")
            .Sum(d => d.RemainingAmount);

        var goalsAverage = goals.Any()
            ? goals.Average(g => g.ProgressPercentage)
            : 0m;

        return new FinancialHealthSummaryDto
        {
            SavingsRate = summary.SavingsRate,
            ActiveBudgetsCount = budgets.Count,
            OverbudgetCount = budgets.Count(b => b.IsOverBudget),
            TotalDebtOwed = totalOwed,
            TotalDebtReceivable = totalReceivable,
            TotalGoalsActive = goals.Count,
            GoalsProgressAverage = Math.Round(goalsAverage, 1)
        };
    }
}
