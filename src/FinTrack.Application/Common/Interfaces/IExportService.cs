using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrack.Application.Reports.DTOs;

namespace FinTrack.Application.Common.Interfaces;

public interface IExportService
{
    Task<byte[]> GenerateTransactionsExcelAsync(IEnumerable<TransactionExportItemDto> transactions, string currency);
    Task<byte[]> GenerateTransactionsCsvAsync(IEnumerable<TransactionExportItemDto> transactions);
    Task<byte[]> GenerateFinancialReportPdfAsync(FinancialReportExportDataDto reportData);
    Task<byte[]> GenerateFinancialReportExcelAsync(FinancialReportExportDataDto reportData);
}
