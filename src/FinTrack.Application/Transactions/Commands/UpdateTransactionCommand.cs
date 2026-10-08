using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Transactions.Commands;

public record UpdateTransactionCommand(
    Guid Id,
    string Description,
    DateTime Date,
    Guid? CategoryId = null,
    string? Tags = null
) : IRequest<Result>;

public class UpdateTransactionHandler : IRequestHandler<UpdateTransactionCommand, Result>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTransactionHandler(IRepository<Account> accountRepo, IUnitOfWork unitOfWork)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateTransactionCommand request, CancellationToken ct)
    {
        var accounts = await _accountRepo.ListAllAsync(ct);
        var account = accounts.FirstOrDefault(a => a.Transactions.Any(t => t.Id == request.Id));

        if (account == null)
            return Result.Fail($"Transacción con ID {request.Id} no encontrada");

        try
        {
            account.UpdateTransaction(request.Id, request.Description, request.Date, request.CategoryId, request.Tags);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }

        _accountRepo.Update(account);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
