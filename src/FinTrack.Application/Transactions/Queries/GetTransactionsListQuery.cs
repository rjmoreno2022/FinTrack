using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Transactions.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Transactions.Queries;

public record GetTransactionsListQuery(
    Guid? AccountId = null,
    Guid? CategoryId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? Type = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<PagedList<TransactionDto>>>;

public class GetTransactionsListHandler : IRequestHandler<GetTransactionsListQuery, Result<PagedList<TransactionDto>>>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IMapper _mapper;

    public GetTransactionsListHandler(IRepository<Account> accountRepo, IMapper mapper)
    {
        _accountRepo = accountRepo;
        _mapper = mapper;
    }

    public async Task<Result<PagedList<TransactionDto>>> Handle(GetTransactionsListQuery request, CancellationToken ct)
    {
        IEnumerable<Transaction> transactionsQuery;

        if (request.AccountId.HasValue)
        {
            var account = await _accountRepo.GetByIdAsync(request.AccountId.Value, ct);
            if (account == null)
                return Result<PagedList<TransactionDto>>.Fail("Cuenta no encontrada");

            transactionsQuery = account.Transactions;
        }
        else
        {
            var allAccounts = await _accountRepo.ListAllAsync(ct);
            transactionsQuery = allAccounts.SelectMany(a => a.Transactions);
        }

        if (request.CategoryId.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.CategoryId == request.CategoryId.Value);
        }

        if (request.StartDate.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.Date >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.Date <= request.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<TransactionType>(request.Type, true, out var filterType))
        {
            transactionsQuery = transactionsQuery.Where(t => t.Type == filterType);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            transactionsQuery = transactionsQuery.Where(t =>
                t.Description.ToLowerInvariant().Contains(search) ||
                (t.Tags != null && t.Tags.ToLowerInvariant().Contains(search)));
        }

        var list = transactionsQuery.OrderByDescending(t => t.Date).ToList();
        var totalCount = list.Count;

        var pagedItems = list
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = _mapper.Map<List<TransactionDto>>(pagedItems);
        var pagedList = new PagedList<TransactionDto>(dtos, totalCount, request.PageNumber, request.PageSize);

        return Result<PagedList<TransactionDto>>.Ok(pagedList);
    }
}
