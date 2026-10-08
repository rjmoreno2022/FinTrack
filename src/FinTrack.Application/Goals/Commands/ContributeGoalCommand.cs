using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Goals.Commands;

public record ContributeGoalCommand(Guid GoalId, decimal Amount) : IRequest<Result>;

public class ContributeGoalHandler : IRequestHandler<ContributeGoalCommand, Result>
{
    private readonly IRepository<Goal> _goalRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ContributeGoalHandler(IRepository<Goal> goalRepo, IUnitOfWork unitOfWork)
    {
        _goalRepo = goalRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ContributeGoalCommand request, CancellationToken ct)
    {
        var goal = await _goalRepo.GetByIdAsync(request.GoalId, ct);
        if (goal == null)
            return Result.Fail("Meta no encontrada");

        try
        {
            goal.Contribute(request.Amount);
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
