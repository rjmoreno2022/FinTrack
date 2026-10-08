using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.Categories.Commands;

public record DeleteCategoryCommand(Guid Id) : IRequest<Result>;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Account> _accountRepo;
    private readonly IRepository<Budget> _budgetRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryHandler(
        IRepository<Category> categoryRepo,
        IRepository<Account> accountRepo,
        IRepository<Budget> budgetRepo,
        IUnitOfWork unitOfWork)
    {
        _categoryRepo = categoryRepo;
        _accountRepo = accountRepo;
        _budgetRepo = budgetRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await _categoryRepo.GetByIdAsync(request.Id, ct);
        if (category == null)
            return Result.Fail($"Categoría con ID {request.Id} no encontrada");

        if (category.IsSystem)
            return Result.Fail("No se pueden eliminar categorías del sistema");

        var allCategories = await _categoryRepo.ListAllAsync(ct);
        if (allCategories.Any(c => c.ParentCategoryId == request.Id))
            return Result.Fail("No se puede eliminar la categoría porque contiene subcategorías asociadas");

        var accounts = await _accountRepo.ListAllAsync(ct);
        if (accounts.Any(a => a.Transactions.Any(t => t.CategoryId == request.Id)))
            return Result.Fail("No se puede eliminar la categoría porque tiene transacciones asociadas");

        var budgets = await _budgetRepo.ListAllAsync(ct);
        if (budgets.Any(b => b.CategoryId == request.Id))
            return Result.Fail("No se puede eliminar la categoría porque está asociada a uno o más presupuestos");

        _categoryRepo.Delete(category);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
