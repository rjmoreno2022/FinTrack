using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Goals.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Goals.Queries;

public record GetGoalsListQuery(bool ActiveOnly = false) : IRequest<Result<List<GoalDto>>>;

public class GetGoalsListHandler : IRequestHandler<GetGoalsListQuery, Result<List<GoalDto>>>
{
    private readonly IRepository<Goal> _goalRepo;
    private readonly IMapper _mapper;

    public GetGoalsListHandler(IRepository<Goal> goalRepo, IMapper mapper)
    {
        _goalRepo = goalRepo;
        _mapper = mapper;
    }

    public async Task<Result<List<GoalDto>>> Handle(GetGoalsListQuery request, CancellationToken ct)
    {
        var goals = await _goalRepo.ListAllAsync(ct);

        if (request.ActiveOnly)
        {
            goals = goals.Where(g => g.Status == Domain.Enums.GoalStatus.Active).ToList();
        }

        var dtos = _mapper.Map<List<GoalDto>>(goals.OrderByDescending(g => g.CreatedAt));
        return Result<List<GoalDto>>.Ok(dtos);
    }
}
