using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Accounts.Queries;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Accounts;

public class GetAccountByIdHandlerTests
{
    private readonly Mock<IRepository<Account>> _accountRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetAccountByIdHandler _handler;

    public GetAccountByIdHandlerTests()
    {
        _accountRepoMock = new Mock<IRepository<Account>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new GetAccountByIdHandler(_accountRepoMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAccountExists_ShouldReturnAccountDto()
    {
        // Arrange
        var account = new Account("Banesco Panamá", AccountType.Bank, Currency.USD, "Cuenta de ahorros");
        var expectedDto = new AccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = "Bank",
            Currency = "USD",
            Description = account.Description
        };

        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _mapperMock.Setup(m => m.Map<AccountDto>(account))
            .Returns(expectedDto);

        var query = new GetAccountByIdQuery(account.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(account.Id);
        result.Value.Name.Should().Be("Banesco Panamá");
        result.Value.Type.Should().Be("Bank");
        result.Value.Currency.Should().Be("USD");
        result.Value.Description.Should().Be("Cuenta de ahorros");
    }

    [Fact]
    public async Task Handle_WhenAccountDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _accountRepoMock.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var query = new GetAccountByIdQuery(missingId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"Cuenta con ID {missingId} no encontrada");
    }
}
