using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Accounts.Commands;

public record TransferFundsCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    decimal? DestinationAmount = null,
    DateTime? Date = null,
    string? Description = null,
    Guid? CategoryId = null,
    string? Tags = null
) : IRequest<Result<Guid>>;

public class TransferFundsHandler : IRequestHandler<TransferFundsCommand, Result<Guid>>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public TransferFundsHandler(IRepository<Account> accountRepo, IUnitOfWork unitOfWork)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(TransferFundsCommand request, CancellationToken ct)
    {
        if (request.SourceAccountId == request.DestinationAccountId)
            return Result<Guid>.Fail("La cuenta de destino debe ser diferente a la cuenta de origen");

        if (request.Amount <= 0)
            return Result<Guid>.Fail("El monto a transferir debe ser mayor a cero");

        var sourceAccount = await _accountRepo.GetByIdAsync(request.SourceAccountId, ct);
        if (sourceAccount == null)
            return Result<Guid>.Fail("Cuenta de origen no encontrada");

        if (!sourceAccount.IsActive)
            return Result<Guid>.Fail("La cuenta de origen está inactiva");

        var destAccount = await _accountRepo.GetByIdAsync(request.DestinationAccountId, ct);
        if (destAccount == null)
            return Result<Guid>.Fail("Cuenta de destino no encontrada");

        if (!destAccount.IsActive)
            return Result<Guid>.Fail("La cuenta de destino está inactiva");

        if (request.Amount > sourceAccount.Balance)
            return Result<Guid>.Fail($"Fondos insuficientes en la cuenta origen. Balance actual: {sourceAccount.Currency} {sourceAccount.Balance:N2}");

        decimal destinationAmount = request.DestinationAmount.HasValue && request.DestinationAmount.Value > 0
            ? request.DestinationAmount.Value
            : request.Amount;

        var transferDate = request.Date ?? DateTime.UtcNow;

        string sourceDescription = string.IsNullOrWhiteSpace(request.Description)
            ? $"Transferencia a {destAccount.Name}"
            : request.Description.Trim();

        string destDescription = string.IsNullOrWhiteSpace(request.Description)
            ? $"Transferencia desde {sourceAccount.Name}"
            : request.Description.Trim();

        var outTx = sourceAccount.AddOutgoingTransfer(
            request.Amount,
            destAccount.Id,
            sourceDescription,
            transferDate,
            request.CategoryId,
            request.Tags);

        var inTx = destAccount.AddIncomingTransfer(
            destinationAmount,
            sourceAccount.Id,
            destDescription,
            transferDate,
            request.CategoryId,
            request.Tags);

        outTx.SetTransferInfo(destAccount.Id, inTx.Id);
        inTx.SetTransferInfo(sourceAccount.Id, outTx.Id);

        _accountRepo.Update(sourceAccount);
        _accountRepo.Update(destAccount);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Ok(outTx.Id);
    }
}
