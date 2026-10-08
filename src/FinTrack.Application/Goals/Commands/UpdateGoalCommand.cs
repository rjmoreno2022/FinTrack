using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Goals.Commands;

public record UpdateGoalCommand(
    Guid Id,
    string Name,
    decimal TargetAmount,
    DateTime? TargetDate = null,
    string? Description = null,
    string Priority = "Medium"
) : IRequest<Result>;

public class UpdateGoalHandler : IRequestHandler<UpdateGoalCommand, Result>
{
    private readonly IRepository<Goal> _goalRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGoalHandler(IRepository<Goal> goalRepo, IUnitOfWork unitOfWork)
    {
        _goalRepo = goalRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateGoalCommand request, CancellationToken ct)
    {
        var goal = await _goalRepo.GetByIdAsync(request.Id, ct);
        if (goal == null)
            return Result.Fail($"Meta con ID {request.Id} no encontrada");

        if (!Enum.TryParse<GoalPriority>(request.Priority, true, out var priority))
            return Result.Fail($"Prioridad '{request.Priority}' no es válida");

        try
        {
            goal.Update(request.Name, request.TargetAmount, request.TargetDate, request.Description, priority);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }

        _goalRepo.Update(goal);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
