using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Reports.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Reports.Queries;

public record ExportFinancialReportPdfQuery(
    string Period = "CurrentMonth",
    string Currency = "USD",
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<Result<ExportFileResultDto>>;

public class ExportFinancialReportPdfQueryHandler : IRequestHandler<ExportFinancialReportPdfQuery, Result<ExportFileResultDto>>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<ExchangeRate> _exchangeRateRepo;
    private readonly IExportService _exportService;

    public ExportFinancialReportPdfQueryHandler(
        IRepository<Account> accountRepo,
        IRepository<Category> categoryRepo,
        IRepository<ExchangeRate> exchangeRateRepo,
        IExportService exportService)
    {
        _accountRepo = accountRepo;
        _categoryRepo = categoryRepo;
        _exchangeRateRepo = exchangeRateRepo;
        _exportService = exportService;
    }

    public async Task<Result<ExportFileResultDto>> Handle(ExportFinancialReportPdfQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        DateTime start;
        DateTime end;
        string periodTitle;

        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            start = request.StartDate.Value;
            end = request.EndDate.Value;
            periodTitle = $"Período Personalizado ({start:dd/MM/yyyy} - {end:dd/MM/yyyy})";
        }
        else
        {
            switch (request.Period)
            {
                case "Last3Months":
                    start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-2);
                    end = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddTicks(-1);
                    periodTitle = "Últimos 3 Meses";
                    break;
                case "Last6Months":
                    start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
                    end = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddTicks(-1);
                    periodTitle = "Últimos 6 Meses";
                    break;
                case "CurrentYear":
                    start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    end = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                    periodTitle = $"Año {now.Year}";
                    break;
                case "CurrentMonth":
                default:
                    start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    end = start.AddMonths(1).AddTicks(-1);
                    periodTitle = $"Mes de {start:MMMM yyyy}";
                    break;
            }
        }

        var accounts = await _accountRepo.ListAllAsync(ct);
        var categories = await _categoryRepo.ListAllAsync(ct);
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        var transactions = accounts
            .SelectMany(a => a.Transactions)
            .Where(t => t.Date >= start && t.Date <= end)
            .OrderByDescending(t => t.Date)
            .ToList();

        var totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var netSavings = totalIncome - totalExpense;
        var savingsRate = totalIncome > 0 ? Math.Max(0, Math.Round((netSavings / totalIncome) * 100, 1)) : 0m;

        // Categorías de gastos
        var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Expense).ToList();
        var categoryItems = new List<CategoryExpenseReportItemDto>();

        if (totalExpense > 0)
        {
            categoryItems = expenseTransactions
                .GroupBy(t => t.CategoryId.HasValue && categoryMap.TryGetValue(t.CategoryId.Value, out var name) ? name : "General")
                .Select(g =>
                {
                    var sum = g.Sum(t => t.Amount);
                    return new CategoryExpenseReportItemDto(
                        CategoryName: g.Key,
                        Amount: sum,
                        Percentage: Math.Round((sum / totalExpense) * 100, 1)
                    );
                })
                .OrderByDescending(c => c.Amount)
                .ToList();
        }

        // Tasas de cambio oficiales actuales
        var rates = await _exchangeRateRepo.ListAllAsync(ct);
        var activeUsd = rates.FirstOrDefault(r => r.FromCurrency == Currency.USD && r.ToCurrency == Currency.VES && r.IsActive)?.Rate ?? 842.21m;
        var activeEur = rates.FirstOrDefault(r => r.FromCurrency == Currency.EUR && r.ToCurrency == Currency.VES && r.IsActive)?.Rate ?? 977.88m;

        var txExportItems = transactions.Take(100).Select(t => new TransactionExportItemDto(
            Id: t.Id,
            Date: t.Date,
            Description: t.Description,
            Type: t.Type.ToString(),
            Amount: t.Amount,
            Currency: request.Currency,
            AccountName: accountMap.TryGetValue(t.AccountId, out var accName) ? accName : "Cuenta",
            CategoryName: t.CategoryId.HasValue && categoryMap.TryGetValue(t.CategoryId.Value, out var cName) ? cName : "General",
            Notes: t.Tags ?? string.Empty
        )).ToList();

        var reportData = new FinancialReportExportDataDto(
            PeriodTitle: periodTitle,
            GeneratedAt: DateTime.UtcNow,
            BaseCurrency: request.Currency,
            TotalIncome: totalIncome,
            TotalExpense: totalExpense,
            NetSavings: netSavings,
            SavingsRate: savingsRate,
            UsdExchangeRate: activeUsd,
            EurExchangeRate: activeEur,
            Categories: categoryItems,
            Transactions: txExportItems
        );

        var pdfBytes = await _exportService.GenerateFinancialReportPdfAsync(reportData);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmm");

        return Result<ExportFileResultDto>.Ok(new ExportFileResultDto(
            Content: pdfBytes,
            ContentType: "application/pdf",
            FileName: $"FinTrack_Estado_Financiero_{timestamp}.pdf"
        ));
    }
}
