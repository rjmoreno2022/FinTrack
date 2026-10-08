using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Goals.Commands;

public record CreateGoalCommand(
    string Name,
    decimal TargetAmount,
    string Currency,
    DateTime? TargetDate = null,
    string? Description = null,
    string Priority = "Medium"
) : IRequest<Result<Guid>>;

public class CreateGoalHandler : IRequestHandler<CreateGoalCommand, Result<Guid>>
{
    private readonly IRepository<Goal> _goalRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGoalHandler(IRepository<Goal> goalRepo, IUnitOfWork unitOfWork)
    {
        _goalRepo = goalRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateGoalCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
            return Result<Guid>.Fail($"Moneda inválida: {request.Currency}");

        if (!Enum.TryParse<GoalPriority>(request.Priority, true, out var priority))
            return Result<Guid>.Fail($"Prioridad inválida: {request.Priority}");

        var goal = new Goal(
            request.Name,
            request.TargetAmount,
            currency,
            request.TargetDate,
            request.Description,
            priority
        );

        await _goalRepo.AddAsync(goal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Ok(goal.Id);
    }
}
