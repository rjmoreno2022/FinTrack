using System;
using System.Threading.Tasks;
using FinTrack.Application.Reports.Queries;
using FinTrack.Application.Transactions.Queries;
using FinTrack.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> ExportTransactions(
        [FromQuery] string format = "xlsx",
        [FromQuery] string? type = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? search = null,
        [FromQuery] string currency = "USD",
        [FromQuery] Guid? accountId = null,
        [FromQuery] Guid? categoryId = null)
    {
        var query = new ExportTransactionsQuery(
            Format: format,
            Type: type,
            StartDate: startDate,
            EndDate: endDate,
            Search: search,
            Currency: currency,
            AccountId: accountId,
            CategoryId: categoryId
        );

        var result = await _mediator.Send(query);
        if (!result.IsSuccess || result.Value == null)
        {
            return BadRequest(new { error = result.Error ?? "Error al exportar transacciones." });
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("financial-report-pdf")]
    public async Task<IActionResult> ExportFinancialReportPdf(
        [FromQuery] string period = "CurrentMonth",
        [FromQuery] string currency = "USD",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new ExportFinancialReportPdfQuery(
            Period: period,
            Currency: currency,
            StartDate: startDate,
            EndDate: endDate
        );

        var result = await _mediator.Send(query);
        if (!result.IsSuccess || result.Value == null)
        {
            return BadRequest(new { error = result.Error ?? "Error al generar estado de cuenta PDF." });
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("financial-report-excel")]
    public async Task<IActionResult> ExportFinancialReportExcel(
        [FromQuery] string period = "CurrentMonth",
        [FromQuery] string currency = "USD",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new ExportFinancialReportExcelQuery(
            Period: period,
            Currency: currency,
            StartDate: startDate,
            EndDate: endDate
        );

        var result = await _mediator.Send(query);
        if (!result.IsSuccess || result.Value == null)
        {
            return BadRequest(new { error = result.Error ?? "Error al generar reporte financiero Excel." });
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }
}
