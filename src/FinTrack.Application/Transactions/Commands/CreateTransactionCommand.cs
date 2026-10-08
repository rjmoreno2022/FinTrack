using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Transactions.Commands;

public record CreateTransactionCommand(
    Guid AccountId,
    string Type,
    decimal Amount,
    string Currency,
    string Description,
    DateTime Date,
    Guid? CategoryId = null,
    string? Tags = null,
    bool IsRecurring = false,
    Guid? TransferAccountId = null
) : IRequest<Result<Guid>>;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, Result<Guid>>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransactionHandler(IRepository<Account> accountRepo, IUnitOfWork unitOfWork)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateTransactionCommand request, CancellationToken ct)
    {
        var account = await _accountRepo.GetByIdAsync(request.AccountId, ct);
        if (account == null)
            return Result<Guid>.Fail("Cuenta no encontrada");

        if (!account.IsActive)
            return Result<Guid>.Fail("No se pueden registrar transacciones en una cuenta inactiva");

        if (!Enum.TryParse<TransactionType>(request.Type, true, out var type))
            return Result<Guid>.Fail($"Tipo de transacción no válido: {request.Type}");

        if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
            return Result<Guid>.Fail($"Moneda no válida: {request.Currency}");

        // Si la moneda difiere de la cuenta para este MVP inicial
        if (currency != account.Currency)
            return Result<Guid>.Fail($"La moneda de la transacción ({currency}) debe coincidir con la de la cuenta ({account.Currency})");

        // Validar fondos suficientes para gastos y transferencias
        if ((type == TransactionType.Expense || type == TransactionType.Transfer) && request.Amount > account.Balance)
        {
            return Result<Guid>.Fail(
                $"Fondos insuficientes. Balance actual: {account.Currency} {account.Balance:N2}");
        }

        var transaction = account.AddTransaction(
            type,
            request.Amount,
            request.Description,
            request.Date,
            request.CategoryId,
            request.Tags,
            request.IsRecurring
        );

        _accountRepo.Update(account);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Ok(transaction.Id);
    }
}
