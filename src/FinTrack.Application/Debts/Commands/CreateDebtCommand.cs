using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Debts.Commands;

public record CreateDebtCommand(
    string Creditor,
    decimal OriginalAmount,
    string Currency,
    string Type,
    DateTime? DueDate = null,
    string? Description = null,
    decimal InterestRate = 0
) : IRequest<Result<Guid>>;

public class CreateDebtHandler : IRequestHandler<CreateDebtCommand, Result<Guid>>
{
    private readonly IRepository<Debt> _debtRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDebtHandler(IRepository<Debt> debtRepo, IUnitOfWork unitOfWork)
    {
        _debtRepo = debtRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateDebtCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
            return Result<Guid>.Fail($"Moneda inválida: {request.Currency}");

        if (!Enum.TryParse<DebtType>(request.Type, true, out var type))
            return Result<Guid>.Fail($"Tipo de deuda inválido: {request.Type}");

        var debt = new Debt(
            request.Creditor,
            request.OriginalAmount,
            currency,
            type,
            request.DueDate,
            request.Description,
            request.InterestRate
        );

        await _debtRepo.AddAsync(debt, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Ok(debt.Id);
    }
}
