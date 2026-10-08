using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrack.Application.Categories.Commands;
using FinTrack.Application.Categories.DTOs;
using FinTrack.Application.Categories.Queries;
using MediatR;

namespace FinTrack.Web.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(string? type = null);
    Task<CategoryDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(CreateCategoryRequest request);
    Task UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task DeleteAsync(Guid id);
}

public class CategoryService : ICategoryService
{
    private readonly IMediator _mediator;

    public CategoryService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<CategoryDto>> GetAllAsync(string? type = null)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(type));
        return result.IsSuccess && result.Value != null
            ? new List<CategoryDto>(result.Value)
            : new List<CategoryDto>();
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id));
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<Guid> CreateAsync(CreateCategoryRequest request)
    {
        var command = new CreateCategoryCommand(request.Name, request.Type, request.Icon, request.Color);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al crear la categoría");

        return result.Value;
    }

    public async Task UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var command = new UpdateCategoryCommand(id, request.Name, request.Icon, request.Color);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al actualizar la categoría");
    }

    public async Task DeleteAsync(Guid id)
    {
        var command = new DeleteCategoryCommand(id);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al eliminar la categoría");
    }
}
