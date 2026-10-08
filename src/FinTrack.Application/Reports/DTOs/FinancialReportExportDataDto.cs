using System;
using System.Collections.Generic;

namespace FinTrack.Application.Reports.DTOs;

public record FinancialReportExportDataDto(
    string PeriodTitle,
    DateTime GeneratedAt,
    string BaseCurrency,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetSavings,
    decimal SavingsRate,
    decimal UsdExchangeRate,
    decimal EurExchangeRate,
    IReadOnlyList<CategoryExpenseReportItemDto> Categories,
    IReadOnlyList<TransactionExportItemDto> Transactions
);

public record CategoryExpenseReportItemDto(
    string CategoryName,
    decimal Amount,
    decimal Percentage
);
