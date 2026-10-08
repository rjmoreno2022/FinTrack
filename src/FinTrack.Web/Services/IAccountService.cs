using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrack.Application.Accounts.Commands;
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Accounts.Queries;
using MediatR;

namespace FinTrack.Web.Services;

public interface IAccountService
{
    Task<List<AccountDto>> GetAllAsync();
    Task<AccountDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(CreateAccountRequest request);
    Task UpdateAsync(Guid id, UpdateAccountRequest request);
    Task DeleteAsync(Guid id);
}

public class AccountService : IAccountService
{
    private readonly IMediator _mediator;

    public AccountService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<AccountDto>> GetAllAsync()
    {
        var result = await _mediator.Send(new GetAccountsListQuery(PageNumber: 1, PageSize: 100));
        return result.IsSuccess && result.Value != null
            ? new List<AccountDto>(result.Value.Items)
            : new List<AccountDto>();
    }

    public async Task<AccountDto?> GetByIdAsync(Guid id)
    {
        var result = await _mediator.Send(new GetAccountByIdQuery(id));
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<Guid> CreateAsync(CreateAccountRequest request)
    {
        var command = new CreateAccountCommand(request.Name, request.Type, request.Currency, request.Description);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al crear la cuenta");

        return result.Value;
    }

    public async Task UpdateAsync(Guid id, UpdateAccountRequest request)
    {
        var command = new UpdateAccountCommand(id, request.Name, request.Description);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al actualizar la cuenta");
    }

    public async Task DeleteAsync(Guid id)
    {
        var command = new DeleteAccountCommand(id);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al eliminar la cuenta");
    }
}
