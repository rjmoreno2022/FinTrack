using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Categories.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Categories;

public class CategoryHandlerTests
{
    private readonly Mock<IRepository<Category>> _categoryRepoMock;
    private readonly Mock<IRepository<Account>> _accountRepoMock;
    private readonly Mock<IRepository<Budget>> _budgetRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public CategoryHandlerTests()
    {
        _categoryRepoMock = new Mock<IRepository<Category>>();
        _accountRepoMock = new Mock<IRepository<Account>>();
        _budgetRepoMock = new Mock<IRepository<Budget>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    [Fact]
    public async Task CreateCategory_WithValidData_ShouldCreateAndReturnId()
    {
        _categoryRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());
        _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateCategoryHandler(_categoryRepoMock.Object, _unitOfWorkMock.Object);
        var command = new CreateCategoryCommand("Mascotas", "Expense", "pets", "#FF5500");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _categoryRepoMock.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "Mascotas"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCategory_WhenDuplicateNameExists_ShouldReturnFailure()
    {
        var existing = new Category("Mascotas", CategoryType.Expense);
        _categoryRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { existing });

        var handler = new CreateCategoryHandler(_categoryRepoMock.Object, _unitOfWorkMock.Object);
        var command = new CreateCategoryCommand("Mascotas", "Expense");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Ya existe una categoría");
        _categoryRepoMock.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategory_WhenIsSystem_ShouldReturnFailure()
    {
        var systemCat = new Category(Guid.NewGuid(), "General", CategoryType.Expense, null, null, null, isSystem: true);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(systemCat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemCat);

        var handler = new UpdateCategoryHandler(_categoryRepoMock.Object, _unitOfWorkMock.Object);
        var command = new UpdateCategoryCommand(systemCat.Id, "Modificada");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("sistema");
        _categoryRepoMock.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategory_WhenIsSystem_ShouldReturnFailure()
    {
        var systemCat = new Category(Guid.NewGuid(), "General", CategoryType.Expense, null, null, null, isSystem: true);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(systemCat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemCat);

        var handler = new DeleteCategoryHandler(
            _categoryRepoMock.Object,
            _accountRepoMock.Object,
            _budgetRepoMock.Object,
            _unitOfWorkMock.Object);

        var command = new DeleteCategoryCommand(systemCat.Id);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("sistema");
        _categoryRepoMock.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategory_WhenHasTransactionsAssociated_ShouldReturnFailure()
    {
        var cat = new Category("Cine", CategoryType.Expense);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(cat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cat);
        _categoryRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { cat });

        var account = new Account("Banesco", AccountType.Bank, Currency.USD);
        account.AddTransaction(TransactionType.Expense, 15m, "Entradas", DateTime.UtcNow, categoryId: cat.Id);

        _accountRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });
        _budgetRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget>());

        var handler = new DeleteCategoryHandler(
            _categoryRepoMock.Object,
            _accountRepoMock.Object,
            _budgetRepoMock.Object,
            _unitOfWorkMock.Object);

        var result = await handler.Handle(new DeleteCategoryCommand(cat.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("transacciones");
        _categoryRepoMock.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategory_WhenUnused_ShouldDeleteSuccessfully()
    {
        var cat = new Category("Temporal", CategoryType.Expense);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(cat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cat);
        _categoryRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { cat });
        _accountRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());
        _budgetRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget>());
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new DeleteCategoryHandler(
            _categoryRepoMock.Object,
            _accountRepoMock.Object,
            _budgetRepoMock.Object,
            _unitOfWorkMock.Object);

        var result = await handler.Handle(new DeleteCategoryCommand(cat.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _categoryRepoMock.Verify(r => r.Delete(cat), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
