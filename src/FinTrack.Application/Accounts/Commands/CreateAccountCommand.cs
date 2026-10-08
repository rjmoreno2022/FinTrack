using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Accounts.Commands;

public record CreateAccountCommand(
    string Name,
    string Type,
    string Currency,
    string? Description
) : IRequest<Result<Guid>>;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Result<Guid>>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountHandler(IRepository<Account> accountRepo, IUnitOfWork unitOfWork)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateAccountCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<AccountType>(request.Type, true, out var accountType))
            return Result<Guid>.Fail($"Tipo de cuenta inválido: {request.Type}");

        if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
            return Result<Guid>.Fail($"Moneda inválida: {request.Currency}");

        var account = new Account(request.Name, accountType, currency, request.Description);

        await _accountRepo.AddAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Ok(account.Id);
    }
}
