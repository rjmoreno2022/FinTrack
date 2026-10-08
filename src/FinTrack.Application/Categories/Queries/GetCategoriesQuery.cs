using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Categories.DTOs;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Categories.Queries;

public record GetCategoriesQuery(string? Type = null) : IRequest<Result<IReadOnlyList<CategoryDto>>>;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IMapper _mapper;

    public GetCategoriesHandler(IRepository<Category> categoryRepo, IMapper mapper)
    {
        _categoryRepo = categoryRepo;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var allCategories = await _categoryRepo.ListAllAsync(ct);

        var query = allCategories.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<CategoryType>(request.Type, true, out var filterType))
        {
            query = query.Where(c => c.Type == filterType);
        }

        // Filtramos sólo las categorías principales (ParentCategoryId == null) para armar el árbol
        var rootCategories = query
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.Name)
            .ToList();

        var dtos = _mapper.Map<List<CategoryDto>>(rootCategories);

        return Result<IReadOnlyList<CategoryDto>>.Ok(dtos);
    }
}
