using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Categories.Commands;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Icon = null,
    string? Color = null
) : IRequest<Result>;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryHandler(IRepository<Category> categoryRepo, IUnitOfWork unitOfWork)
    {
        _categoryRepo = categoryRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await _categoryRepo.GetByIdAsync(request.Id, ct);
        if (category == null)
            return Result.Fail($"Categoría con ID {request.Id} no encontrada");

        if (category.IsSystem)
            return Result.Fail("No se pueden modificar categorías del sistema");

        var all = await _categoryRepo.ListAllAsync(ct);
        if (all.Any(c => c.Id != request.Id && c.Type == category.Type && c.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result.Fail($"Ya existe otra categoría '{request.Name.Trim()}' para este tipo");

        try
        {
            category.Update(request.Name.Trim(), request.Icon, request.Color);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }

        _categoryRepo.Update(category);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
