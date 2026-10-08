using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Debts.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Debts.Queries;

public record GetDebtByIdQuery(Guid Id) : IRequest<Result<DebtDetailDto>>;

public class GetDebtByIdHandler : IRequestHandler<GetDebtByIdQuery, Result<DebtDetailDto>>
{
    private readonly IRepository<Debt> _debtRepo;
    private readonly IMapper _mapper;

    public GetDebtByIdHandler(IRepository<Debt> debtRepo, IMapper mapper)
    {
        _debtRepo = debtRepo;
        _mapper = mapper;
    }

    public async Task<Result<DebtDetailDto>> Handle(GetDebtByIdQuery request, CancellationToken ct)
    {
        var debt = await _debtRepo.GetByIdAsync(request.Id, ct);
        if (debt == null)
            return Result<DebtDetailDto>.Fail("Deuda no encontrada");

        var dto = _mapper.Map<DebtDetailDto>(debt);
        return Result<DebtDetailDto>.Ok(dto);
    }
}
