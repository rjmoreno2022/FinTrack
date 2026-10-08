using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Categories.Commands;

public record CreateCategoryCommand(
    string Name,
    string Type,
    string? Icon = null,
    string? Color = null
) : IRequest<Result<Guid>>;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryHandler(IRepository<Category> categoryRepo, IUnitOfWork unitOfWork)
    {
        _categoryRepo = categoryRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<CategoryType>(request.Type, true, out var type))
            return Result<Guid>.Fail($"Tipo de categoría '{request.Type}' no es válido");

        var all = await _categoryRepo.ListAllAsync(ct);
        if (all.Any(c => c.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase) && c.Type == type))
            return Result<Guid>.Fail($"Ya existe una categoría '{request.Name.Trim()}' para el tipo {type}");

        Category category;
        try
        {
            category = new Category(request.Name.Trim(), type, request.Icon, request.Color, parentCategoryId: null, isSystem: false);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Fail(ex.Message);
        }

        await _categoryRepo.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Ok(category.Id);
    }
}
