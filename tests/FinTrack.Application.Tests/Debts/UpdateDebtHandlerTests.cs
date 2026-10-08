using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Debts.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Debts;

public class UpdateDebtHandlerTests
{
    private readonly Mock<IRepository<Debt>> _debtRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateDebtHandler _handler;

    public UpdateDebtHandlerTests()
    {
        _debtRepoMock = new Mock<IRepository<Debt>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateDebtHandler(_debtRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateDebtAndReturnSuccess()
    {
        var debt = new Debt("Pedro", 300m, Currency.USD, DebtType.Owed);

        _debtRepoMock.Setup(r => r.GetByIdAsync(debt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(debt);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var newDueDate = DateTime.UtcNow.AddMonths(2);
        var command = new UpdateDebtCommand(debt.Id, "Pedro Gómez", newDueDate, "Préstamo arreglado", 3m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        debt.Creditor.Should().Be("Pedro Gómez");
        debt.DueDate.Should().Be(newDueDate);
        debt.Description.Should().Be("Préstamo arreglado");
        debt.InterestRate.Should().Be(3m);
        _debtRepoMock.Verify(r => r.Update(debt), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ShouldReturnFailure()
    {
        _debtRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Debt?)null);

        var command = new UpdateDebtCommand(Guid.NewGuid(), "Acreedor");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no encontrada");
    }
}
