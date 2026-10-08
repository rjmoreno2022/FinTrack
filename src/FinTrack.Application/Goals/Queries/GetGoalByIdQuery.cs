using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Goals.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Goals.Queries;

public record GetGoalByIdQuery(Guid Id) : IRequest<Result<GoalDto>>;

public class GetGoalByIdHandler : IRequestHandler<GetGoalByIdQuery, Result<GoalDto>>
{
    private readonly IRepository<Goal> _goalRepo;
    private readonly IMapper _mapper;

    public GetGoalByIdHandler(IRepository<Goal> goalRepo, IMapper mapper)
    {
        _goalRepo = goalRepo;
        _mapper = mapper;
    }

    public async Task<Result<GoalDto>> Handle(GetGoalByIdQuery request, CancellationToken ct)
    {
        var goal = await _goalRepo.GetByIdAsync(request.Id, ct);
        if (goal == null)
            return Result<GoalDto>.Fail("Meta no encontrada");

        var dto = _mapper.Map<GoalDto>(goal);
        return Result<GoalDto>.Ok(dto);
    }
}
