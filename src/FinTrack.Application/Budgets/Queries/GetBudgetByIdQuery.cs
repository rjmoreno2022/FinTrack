using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Budgets.DTOs;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Budgets.Queries;

public record GetBudgetByIdQuery(Guid Id) : IRequest<Result<BudgetDto>>;

public class GetBudgetByIdHandler : IRequestHandler<GetBudgetByIdQuery, Result<BudgetDto>>
{
    private readonly IRepository<Budget> _budgetRepo;
    private readonly IMapper _mapper;

    public GetBudgetByIdHandler(IRepository<Budget> budgetRepo, IMapper mapper)
    {
        _budgetRepo = budgetRepo;
        _mapper = mapper;
    }

    public async Task<Result<BudgetDto>> Handle(GetBudgetByIdQuery request, CancellationToken ct)
    {
        var budget = await _budgetRepo.GetByIdAsync(request.Id, ct);
        if (budget == null)
            return Result<BudgetDto>.Fail($"Presupuesto con ID {request.Id} no encontrado");

        var dto = _mapper.Map<BudgetDto>(budget);
        return Result<BudgetDto>.Ok(dto);
    }
}
