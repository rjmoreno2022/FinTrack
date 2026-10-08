using System;
using System.Threading.Tasks;
using FinTrack.Application.Debts.Commands;
using FinTrack.Application.Debts.DTOs;
using FinTrack.Application.Debts.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebtsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DebtsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type = null, [FromQuery] string? status = null)
    {
        var result = await _mediator.Send(new GetDebtsListQuery(type, status));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetDebtByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDebtRequest request)
    {
        var command = new CreateDebtCommand(
            request.Creditor,
            request.OriginalAmount,
            request.Currency,
            request.Type,
            request.DueDate,
            request.Description,
            request.InterestRate
        );

        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
            : BadRequest(new { error = result.Error, errors = result.Errors });
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RegisterPayment(Guid id, [FromBody] RegisterDebtPaymentRequest request)
    {
        var command = new RegisterDebtPaymentCommand(id, request.Amount, request.Date, request.Note);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDebtRequest request)
    {
        var command = new UpdateDebtCommand(
            id,
            request.Creditor,
            request.DueDate,
            request.Description,
            request.InterestRate
        );

        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { error = result.Error, errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteDebtCommand(id));
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
