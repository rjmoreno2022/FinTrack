using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Accounts.Queries;

public record GetAccountByIdQuery(Guid Id) : IRequest<Result<AccountDto>>;

public class GetAccountByIdHandler : IRequestHandler<GetAccountByIdQuery, Result<AccountDto>>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IMapper _mapper;

    public GetAccountByIdHandler(IRepository<Account> accountRepo, IMapper mapper)
    {
        _accountRepo = accountRepo;
        _mapper = mapper;
    }

    public async Task<Result<AccountDto>> Handle(GetAccountByIdQuery request, CancellationToken ct)
    {
        var account = await _accountRepo.GetByIdAsync(request.Id, ct);
        if (account == null)
            return Result<AccountDto>.Fail($"Cuenta con ID {request.Id} no encontrada");

        var dto = _mapper.Map<AccountDto>(account);
        return Result<AccountDto>.Ok(dto);
    }
}
