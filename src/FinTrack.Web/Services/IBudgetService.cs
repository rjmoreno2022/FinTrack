using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrack.Application.Budgets.Commands;
using FinTrack.Application.Budgets.DTOs;
using FinTrack.Application.Budgets.Queries;
using MediatR;

namespace FinTrack.Web.Services;

public interface IBudgetService
{
    Task<List<BudgetDto>> GetAllAsync(bool activeOnly = false);
    Task<Guid> CreateAsync(CreateBudgetRequest request);
    Task DeleteAsync(Guid id);
}

public class BudgetService : IBudgetService
{
    private readonly IMediator _mediator;

    public BudgetService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<BudgetDto>> GetAllAsync(bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetBudgetsListQuery(activeOnly));
        return result.IsSuccess && result.Value != null
            ? result.Value
            : new List<BudgetDto>();
    }

    public async Task<Guid> CreateAsync(CreateBudgetRequest request)
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
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al crear presupuesto");

        return result.Value;
    }

    public async Task DeleteAsync(Guid id)
    {
        var command = new DeleteBudgetCommand(id);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al eliminar presupuesto");
    }
}
