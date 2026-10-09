using System;
using System.Threading.Tasks;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Auth;

public class MultiUserIsolationTests
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    private FinTrackDbContext CreateDbContext(Guid? currentUserId)
    {
        var options = new DbContextOptionsBuilder<FinTrackDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.UserId).Returns(currentUserId);
        userMock.Setup(u => u.IsAuthenticated).Returns(currentUserId.HasValue);

        var context = new FinTrackDbContext(options, userMock.Object);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldAutomaticallyAssignUserIdToIUserOwnedEntities()
    {
        // Arrange
        var userAId = Guid.NewGuid();
        using var context = CreateDbContext(userAId);

        var account = new Account("Banesco USD", AccountType.Bank, Currency.USD);
        var budget = new Budget("Alimentación Mes", 300m, Currency.USD, BudgetPeriod.Monthly, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));
        var goal = new Goal("Fondo de Emergencia", 1000m, Currency.USD, DateTime.UtcNow.AddYears(1));
        var debt = new Debt("Préstamo Familiar", 500m, Currency.USD, DebtType.Owed, DateTime.UtcNow.AddMonths(6));

        // Act
        context.Accounts.Add(account);
        context.Budgets.Add(budget);
        context.Goals.Add(goal);
        context.Debts.Add(debt);
        await context.SaveChangesAsync();

        // Assert
        account.UserId.Should().Be(userAId);
        budget.UserId.Should().Be(userAId);
        goal.UserId.Should().Be(userAId);
        debt.UserId.Should().Be(userAId);
    }

    [Fact]
    public async Task GlobalQueryFilters_ShouldIsolateAccountsBetweenUsers()
    {
        // Arrange
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        // User A creates an account
        using (var contextA = CreateDbContext(userAId))
        {
            var accountA = new Account("Cuenta de A", AccountType.Bank, Currency.USD);
            contextA.Accounts.Add(accountA);
            await contextA.SaveChangesAsync();
        }

        // User B creates an account
        using (var contextB = CreateDbContext(userBId))
        {
            var accountB = new Account("Cuenta de B", AccountType.Cash, Currency.VES);
            contextB.Accounts.Add(accountB);
            await contextB.SaveChangesAsync();
        }

        // Assert User A queries
        using (var contextA = CreateDbContext(userAId))
        {
            var accountsA = await contextA.Accounts.ToListAsync();
            accountsA.Should().ContainSingle();
            accountsA[0].Name.Should().Be("Cuenta de A");
            accountsA[0].UserId.Should().Be(userAId);
        }

        // Assert User B queries
        using (var contextB = CreateDbContext(userBId))
        {
            var accountsB = await contextB.Accounts.ToListAsync();
            accountsB.Should().ContainSingle();
            accountsB[0].Name.Should().Be("Cuenta de B");
            accountsB[0].UserId.Should().Be(userBId);
        }
    }

    [Fact]
    public async Task Categories_SystemCategoriesShouldBeVisibleToAllUsers_CustomCategoriesOnlyToOwner()
    {
        // Arrange
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        // User A creates a custom category
        using (var contextA = CreateDbContext(userAId))
        {
            var customCategoryA = new Category("Cursos Online de A", CategoryType.Expense, icon: "school");
            contextA.Categories.Add(customCategoryA);
            await contextA.SaveChangesAsync();
        }

        // User A should see system categories + their own custom category
        using (var contextA = CreateDbContext(userAId))
        {
            var categoriesA = await contextA.Categories.ToListAsync();
            categoriesA.Should().Contain(c => c.Name == "Cursos Online de A");
            categoriesA.Should().Contain(c => c.IsSystem);
        }

        // User B should see system categories, but NOT User A's custom category
        using (var contextB = CreateDbContext(userBId))
        {
            var categoriesB = await contextB.Categories.ToListAsync();
            categoriesB.Should().NotContain(c => c.Name == "Cursos Online de A");
            categoriesB.Should().Contain(c => c.IsSystem);
        }
    }

    [Fact]
    public async Task Transactions_ShouldOnlyBeVisibleToAccountOwner()
    {
        // Arrange
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        using (var contextA = CreateDbContext(userAId))
        {
            var accountA = new Account("Banesco de A", AccountType.Bank, Currency.USD);
            accountA.AddTransaction(TransactionType.Income, 500m, "Sueldo de A", DateTime.UtcNow);
            contextA.Accounts.Add(accountA);
            await contextA.SaveChangesAsync();
        }

        // User A sees their transaction
        using (var contextA = CreateDbContext(userAId))
        {
            var txsA = await contextA.Transactions.ToListAsync();
            txsA.Should().ContainSingle();
            txsA[0].Description.Should().Be("Sueldo de A");
        }

        // User B sees 0 transactions
        using (var contextB = CreateDbContext(userBId))
        {
            var txsB = await contextB.Transactions.ToListAsync();
            txsB.Should().BeEmpty();
        }
    }
}
