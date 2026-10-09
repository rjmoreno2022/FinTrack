using System;
using System.Threading.Tasks;
using FinTrack.Application.Budgets.Commands;
using FinTrack.Application.Budgets.DTOs;
using FinTrack.Application.Budgets.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BudgetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetBudgetsListQuery(activeOnly));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request)
    {
        var command = new CreateBudgetCommand(
            request.Name,
            request.LimitAmount,
            request.Currency,
            request.Period,
            request.StartDate,
            request.EndDate,
            request.CategoryId
        );

        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAll), new { id = result.Value }, new { id = result.Value })
            : BadRequest(new { error = result.Error, errors = result.Errors });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetBudgetByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        var command = new UpdateBudgetCommand(
            id,
            request.Name,
            request.LimitAmount,
            request.StartDate,
            request.EndDate,
            request.CategoryId
        );

        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { error = result.Error, errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteBudgetCommand(id));
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
