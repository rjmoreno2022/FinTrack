using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Goals.Commands;

public record DeleteGoalCommand(Guid Id) : IRequest<Result>;

public class DeleteGoalHandler : IRequestHandler<DeleteGoalCommand, Result>
{
    private readonly IRepository<Goal> _goalRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGoalHandler(IRepository<Goal> goalRepo, IUnitOfWork unitOfWork)
    {
        _goalRepo = goalRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteGoalCommand request, CancellationToken ct)
    {
        var goal = await _goalRepo.GetByIdAsync(request.Id, ct);
        if (goal == null)
            return Result.Fail("Meta no encontrada");

        _goalRepo.Delete(goal);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
