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

namespace FinTrack.Application.Transactions.Queries;

public record ExportTransactionsQuery(
    string Format = "xlsx",
    string? Type = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    string? Search = null,
    string Currency = "USD"
) : IRequest<Result<ExportFileResultDto>>;

public class ExportTransactionsQueryHandler : IRequestHandler<ExportTransactionsQuery, Result<ExportFileResultDto>>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IExportService _exportService;

    public ExportTransactionsQueryHandler(
        IRepository<Account> accountRepo,
        IRepository<Category> categoryRepo,
        IExportService exportService)
    {
        _accountRepo = accountRepo;
        _categoryRepo = categoryRepo;
        _exportService = exportService;
    }

    public async Task<Result<ExportFileResultDto>> Handle(ExportTransactionsQuery request, CancellationToken ct)
    {
        var accounts = await _accountRepo.ListAllAsync(ct);
        var categories = await _categoryRepo.ListAllAsync(ct);
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);
        var accountMap = accounts.ToDictionary(a => a.Id, a => new { a.Name, Currency = a.Currency.ToString() });

        IEnumerable<Transaction> query;
        if (request.AccountId.HasValue)
        {
            var acc = accounts.FirstOrDefault(a => a.Id == request.AccountId.Value);
            if (acc == null)
                return Result<ExportFileResultDto>.Fail("Cuenta no encontrada");

            query = acc.Transactions;
        }
        else
        {
            query = accounts.SelectMany(a => a.Transactions);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(t => t.Date >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(t => t.Date <= request.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<TransactionType>(request.Type, true, out var filterType))
        {
            query = query.Where(t => t.Type == filterType);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(t =>
                t.Description.ToLowerInvariant().Contains(search) ||
                (t.Tags != null && t.Tags.ToLowerInvariant().Contains(search)));
        }

        var items = query
            .OrderByDescending(t => t.Date)
            .Select(t =>
            {
                var acc = accountMap.TryGetValue(t.AccountId, out var aInfo) ? aInfo : null;
                var catName = t.CategoryId.HasValue && categoryMap.TryGetValue(t.CategoryId.Value, out var cName)
                    ? cName
                    : "General";

                return new TransactionExportItemDto(
                    Id: t.Id,
                    Date: t.Date,
                    Description: t.Description,
                    Type: t.Type.ToString(),
                    Amount: t.Amount,
                    Currency: acc != null ? acc.Currency : request.Currency,
                    AccountName: acc != null ? acc.Name : "Desconocida",
                    CategoryName: catName,
                    Notes: t.Tags ?? string.Empty
                );
            })
            .ToList();

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmm");
        var isCsv = string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase);

        if (isCsv)
        {
            var bytes = await _exportService.GenerateTransactionsCsvAsync(items);
            return Result<ExportFileResultDto>.Ok(new ExportFileResultDto(
                Content: bytes,
                ContentType: "text/csv; charset=utf-8",
                FileName: $"FinTrack_Transacciones_{timestamp}.csv"
            ));
        }
        else
        {
            var bytes = await _exportService.GenerateTransactionsExcelAsync(items, request.Currency);
            return Result<ExportFileResultDto>.Ok(new ExportFileResultDto(
                Content: bytes,
                ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName: $"FinTrack_Transacciones_{timestamp}.xlsx"
            ));
        }
    }
}
