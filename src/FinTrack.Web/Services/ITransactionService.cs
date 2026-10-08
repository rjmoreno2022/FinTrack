using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrack.Application.Categories.DTOs;
using FinTrack.Application.Categories.Queries;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Transactions.Commands;
using FinTrack.Application.Transactions.DTOs;
using FinTrack.Application.Transactions.Queries;
using MediatR;

namespace FinTrack.Web.Services;

public interface ITransactionService
{
    Task<PagedList<TransactionDto>> GetAllAsync(
        Guid? accountId = null,
        Guid? categoryId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? type = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 50);

    Task<Guid> CreateAsync(CreateTransactionRequest request);
    Task DeleteAsync(Guid id);
    Task<List<CategoryDto>> GetCategoriesAsync(string? type = null);
}

public class TransactionService : ITransactionService
{
    private readonly IMediator _mediator;

    public TransactionService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<PagedList<TransactionDto>> GetAllAsync(
        Guid? accountId = null,
        Guid? categoryId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? type = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var query = new GetTransactionsListQuery(
            accountId, categoryId, startDate, endDate, type, search, pageNumber, pageSize);
        var result = await _mediator.Send(query);

        return result.IsSuccess && result.Value != null
            ? result.Value
            : new PagedList<TransactionDto>(new List<TransactionDto>(), 0, pageNumber, pageSize);
    }

    public async Task<Guid> CreateAsync(CreateTransactionRequest request)
    {
        var command = new CreateTransactionCommand(
            request.AccountId,
            request.Type,
            request.Amount,
            request.Currency,
            request.Description,
            request.Date,
            request.CategoryId,
            request.Tags,
            request.IsRecurring,
            request.TransferAccountId
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al registrar la transacción");

        return result.Value;
    }

    public async Task DeleteAsync(Guid id)
    {
        var command = new DeleteTransactionCommand(id);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al eliminar la transacción");
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(string? type = null)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(type));
        return result.IsSuccess && result.Value != null
            ? new List<CategoryDto>(result.Value)
            : new List<CategoryDto>();
    }
}
