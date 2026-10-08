using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Accounts.Queries;

public record GetAccountsListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null
) : IRequest<Result<PagedList<AccountDto>>>;

public class GetAccountsListHandler : IRequestHandler<GetAccountsListQuery, Result<PagedList<AccountDto>>>
{
    private readonly IRepository<Account> _accountRepo;
    private readonly IMapper _mapper;

    public GetAccountsListHandler(IRepository<Account> accountRepo, IMapper mapper)
    {
        _accountRepo = accountRepo;
        _mapper = mapper;
    }

    public async Task<Result<PagedList<AccountDto>>> Handle(GetAccountsListQuery request, CancellationToken ct)
    {
        var allAccounts = await _accountRepo.ListAllAsync(ct);

        var query = allAccounts.Where(a => a.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(a => a.Name.ToLowerInvariant().Contains(search) ||
                                     (a.Description != null && a.Description.ToLowerInvariant().Contains(search)));
        }

        var totalCount = query.Count();
        var pagedItems = query
            .OrderBy(a => a.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = _mapper.Map<List<AccountDto>>(pagedItems);
        var pagedList = new PagedList<AccountDto>(dtos, totalCount, request.PageNumber, request.PageSize);

        return Result<PagedList<AccountDto>>.Ok(pagedList);
    }
}
