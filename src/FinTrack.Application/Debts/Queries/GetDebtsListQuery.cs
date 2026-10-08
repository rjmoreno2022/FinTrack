using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Debts.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Debts.Queries;

public record GetDebtsListQuery(string? Type = null, string? Status = null) : IRequest<Result<List<DebtDto>>>;

public class GetDebtsListHandler : IRequestHandler<GetDebtsListQuery, Result<List<DebtDto>>>
{
    private readonly IRepository<Debt> _debtRepo;
    private readonly IMapper _mapper;

    public GetDebtsListHandler(IRepository<Debt> debtRepo, IMapper mapper)
    {
        _debtRepo = debtRepo;
        _mapper = mapper;
    }

    public async Task<Result<List<DebtDto>>> Handle(GetDebtsListQuery request, CancellationToken ct)
    {
        var debts = await _debtRepo.ListAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(request.Type) &&
            System.Enum.TryParse<Domain.Enums.DebtType>(request.Type, true, out var debtType))
        {
            debts = debts.Where(d => d.Type == debtType).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            System.Enum.TryParse<Domain.Enums.DebtStatus>(request.Status, true, out var debtStatus))
        {
            debts = debts.Where(d => d.Status == debtStatus).ToList();
        }

        var dtos = _mapper.Map<List<DebtDto>>(debts.OrderByDescending(d => d.CreatedAt));
        return Result<List<DebtDto>>.Ok(dtos);
    }
}
