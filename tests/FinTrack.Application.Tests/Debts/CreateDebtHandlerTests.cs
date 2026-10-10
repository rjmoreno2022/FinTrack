using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Debts.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Debts;

public class CreateDebtHandlerTests
{
    private readonly Mock<IRepository<Debt>> _debtRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task CreateDebt_WithValidData_ShouldPersistAndReturnId()
    {
        // Arrange
        var handler = new CreateDebtHandler(_debtRepoMock.Object, _unitOfWorkMock.Object);
        var command = new CreateDebtCommand("Banco Mercantil", 500m, "USD", "Owed", DateTime.UtcNow.AddMonths(3), "Préstamo", 5m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _debtRepoMock.Verify(r => r.AddAsync(It.IsAny<Debt>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDebt_WithInvalidCurrency_ShouldFail()
    {
        // Arrange
        var handler = new CreateDebtHandler(_debtRepoMock.Object, _unitOfWorkMock.Object);
        var command = new CreateDebtCommand("Prestamista", 100m, "INVALID_CURRENCY", "Payable");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Moneda inválida");
    }

    [Fact]
    public async Task CreateDebt_WithInvalidType_ShouldFail()
    {
        // Arrange
        var handler = new CreateDebtHandler(_debtRepoMock.Object, _unitOfWorkMock.Object);
        var command = new CreateDebtCommand("Prestamista", 100m, "USD", "INVALID_TYPE");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Tipo de deuda inválido");
    }
}
