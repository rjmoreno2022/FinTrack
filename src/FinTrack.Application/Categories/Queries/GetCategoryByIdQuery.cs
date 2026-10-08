using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Categories.DTOs;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Categories.Queries;

public record GetCategoryByIdQuery(Guid Id) : IRequest<Result<CategoryDto>>;

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IMapper _mapper;

    public GetCategoryByIdHandler(IRepository<Category> categoryRepo, IMapper mapper)
    {
        _categoryRepo = categoryRepo;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken ct)
    {
        var category = await _categoryRepo.GetByIdAsync(request.Id, ct);
        if (category == null)
            return Result<CategoryDto>.Fail($"Categoría con ID {request.Id} no encontrada");

        var dto = _mapper.Map<CategoryDto>(category);
        return Result<CategoryDto>.Ok(dto);
    }
}
