using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Accounts.Commands;

public record UpdateAccountCommand(
    Guid Id,
    string Name,
    string? Description
) : IRequest<Result>;

public class UpdateAccountHandler : IRequestHandler<UpdateAccountCommand, Result>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAccountHandler(IRepository<Account> accountRepo, IUnitOfWork unitOfWork)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var account = await _accountRepo.GetByIdAsync(request.Id, ct);
        if (account == null)
            return Result.Fail($"Cuenta con ID {request.Id} no encontrada");

        account.UpdateInfo(request.Name, request.Description);

        _accountRepo.Update(account);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
