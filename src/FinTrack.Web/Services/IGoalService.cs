using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrack.Application.Goals.Commands;
using FinTrack.Application.Goals.DTOs;
using FinTrack.Application.Goals.Queries;
using MediatR;

namespace FinTrack.Web.Services;

public interface IGoalService
{
    Task<List<GoalDto>> GetAllAsync(bool activeOnly = false);
    Task<GoalDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(CreateGoalRequest request);
    Task UpdateAsync(Guid id, UpdateGoalRequest request);
    Task ContributeAsync(Guid id, decimal amount);
    Task DeleteAsync(Guid id);
}

public class GoalService : IGoalService
{
    private readonly IMediator _mediator;

    public GoalService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<GoalDto>> GetAllAsync(bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetGoalsListQuery(activeOnly));
        return result.IsSuccess && result.Value != null
            ? result.Value
            : new List<GoalDto>();
    }

    public async Task<GoalDto?> GetByIdAsync(Guid id)
    {
        var result = await _mediator.Send(new GetGoalByIdQuery(id));
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<Guid> CreateAsync(CreateGoalRequest request)
    {
        var command = new CreateGoalCommand(
            request.Name,
            request.TargetAmount,
            request.Currency,
            request.TargetDate,
            request.Description,
            request.Priority
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al crear meta de ahorro");

        return result.Value;
    }

    public async Task UpdateAsync(Guid id, UpdateGoalRequest request)
    {
        var command = new UpdateGoalCommand(
            id,
            request.Name,
            request.TargetAmount,
            request.TargetDate,
            request.Description,
            request.Priority
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al actualizar la meta");
    }

    public async Task ContributeAsync(Guid id, decimal amount)
    {
        var command = new ContributeGoalCommand(id, amount);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al aportar a la meta");
    }

    public async Task DeleteAsync(Guid id)
    {
        var command = new DeleteGoalCommand(id);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al eliminar la meta");
    }
}
