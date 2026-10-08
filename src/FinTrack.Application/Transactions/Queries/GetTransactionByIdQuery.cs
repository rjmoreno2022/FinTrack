using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Transactions.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Transactions.Queries;

public record GetTransactionByIdQuery(Guid Id) : IRequest<Result<TransactionDto>>;

public class GetTransactionByIdHandler : IRequestHandler<GetTransactionByIdQuery, Result<TransactionDto>>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IMapper _mapper;

    public GetTransactionByIdHandler(IRepository<Account> accountRepo, IMapper mapper)
    {
        _accountRepo = accountRepo;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(GetTransactionByIdQuery request, CancellationToken ct)
    {
        var accounts = await _accountRepo.ListAllAsync(ct);
        var transaction = accounts.SelectMany(a => a.Transactions)
                                  .FirstOrDefault(t => t.Id == request.Id);

        if (transaction == null)
            return Result<TransactionDto>.Fail($"Transacción con ID {request.Id} no encontrada");

        var dto = _mapper.Map<TransactionDto>(transaction);
        return Result<TransactionDto>.Ok(dto);
    }
}
