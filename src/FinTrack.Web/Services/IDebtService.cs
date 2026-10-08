using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrack.Application.Debts.Commands;
using FinTrack.Application.Debts.DTOs;
using FinTrack.Application.Debts.Queries;
using MediatR;

namespace FinTrack.Web.Services;

public interface IDebtService
{
    Task<List<DebtDto>> GetAllAsync(string? type = null, string? status = null);
    Task<DebtDetailDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(CreateDebtRequest request);
    Task UpdateAsync(Guid id, UpdateDebtRequest request);
    Task RegisterPaymentAsync(Guid id, RegisterDebtPaymentRequest request);
    Task DeleteAsync(Guid id);
}

public class DebtService : IDebtService
{
    private readonly IMediator _mediator;

    public DebtService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<DebtDto>> GetAllAsync(string? type = null, string? status = null)
    {
        var result = await _mediator.Send(new GetDebtsListQuery(type, status));
        return result.IsSuccess && result.Value != null
            ? result.Value
            : new List<DebtDto>();
    }

    public async Task<DebtDetailDto?> GetByIdAsync(Guid id)
    {
        var result = await _mediator.Send(new GetDebtByIdQuery(id));
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<Guid> CreateAsync(CreateDebtRequest request)
    {
        var command = new CreateDebtCommand(
            request.Creditor,
            request.OriginalAmount,
            request.Currency,
            request.Type,
            request.DueDate,
            request.Description,
            request.InterestRate
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al crear la deuda");

        return result.Value;
    }

    public async Task UpdateAsync(Guid id, UpdateDebtRequest request)
    {
        var command = new UpdateDebtCommand(
            id,
            request.Creditor,
            request.DueDate,
            request.Description,
            request.InterestRate
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al actualizar la deuda");
    }

    public async Task RegisterPaymentAsync(Guid id, RegisterDebtPaymentRequest request)
    {
        var command = new RegisterDebtPaymentCommand(id, request.Amount, request.Date, request.Note);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al registrar abono");
    }

    public async Task DeleteAsync(Guid id)
    {
        var command = new DeleteDebtCommand(id);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al eliminar la deuda");
    }
}
