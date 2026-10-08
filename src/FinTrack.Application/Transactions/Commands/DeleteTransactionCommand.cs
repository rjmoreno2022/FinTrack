using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Transactions.Commands;

public record DeleteTransactionCommand(Guid Id, Guid? AccountId = null) : IRequest<Result>;

public class DeleteTransactionHandler : IRequestHandler<DeleteTransactionCommand, Result>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTransactionHandler(IRepository<Account> accountRepo, IUnitOfWork unitOfWork)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteTransactionCommand request, CancellationToken ct)
    {
        Account? account = null;

        if (request.AccountId.HasValue)
        {
            account = await _accountRepo.GetByIdAsync(request.AccountId.Value, ct);
        }
        else
        {
            var accounts = await _accountRepo.ListAllAsync(ct);
            account = accounts.FirstOrDefault(a => a.Transactions.Any(t => t.Id == request.Id));
        }

        if (account == null)
            return Result.Fail("No se encontró la cuenta o la transacción solicitada");

        account.RemoveTransaction(request.Id);

        _accountRepo.Update(account);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
