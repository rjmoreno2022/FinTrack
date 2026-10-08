using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FinTrack.Application.Common.Mappings;
using FinTrack.Application.ExchangeRates.Commands;
using FinTrack.Application.ExchangeRates.DTOs;
using FinTrack.Application.ExchangeRates.Queries;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.ExchangeRates;

public class ExchangeRateHandlerTests
{
    private readonly Mock<IRepository<ExchangeRate>> _rateRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;

    public ExchangeRateHandlerTests()
    {
        _rateRepoMock = new Mock<IRepository<ExchangeRate>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
    }

    [Fact]
    public async Task CreateExchangeRate_WithValidData_ShouldDeactivateOldAndSaveNew()
    {
        // Arrange
        var oldRate = new ExchangeRate(Currency.USD, Currency.VES, 55m, RateSource.BCV);
        _rateRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExchangeRate> { oldRate });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateExchangeRateHandler(_rateRepoMock.Object, _unitOfWorkMock.Object);
        var command = new CreateExchangeRateCommand("USD", "VES", 60m, "BCV");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        oldRate.IsActive.Should().BeFalse();
        _rateRepoMock.Verify(r => r.Update(oldRate), Times.Once);
        _rateRepoMock.Verify(r => r.AddAsync(It.Is<ExchangeRate>(rate =>
            rate.FromCurrency == Currency.USD &&
            rate.ToCurrency == Currency.VES &&
            rate.Rate == 60m &&
            rate.Source == RateSource.BCV &&
            rate.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLatestRate_WithDirectRate_ShouldReturnDirect()
    {
        // Arrange
        var directRate = new ExchangeRate(Currency.USD, Currency.VES, 60m, RateSource.BCV);
        _rateRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExchangeRate> { directRate });
        _mapperMock.Setup(m => m.Map<ExchangeRateDto>(directRate))
            .Returns(new ExchangeRateDto
            {
                FromCurrency = "USD",
                ToCurrency = "VES",
                Rate = 60m,
                Source = "BCV",
                IsActive = true
            });

        var handler = new GetLatestExchangeRateHandler(_rateRepoMock.Object, _mapperMock.Object);
        var query = new GetLatestExchangeRateQuery("USD", "VES");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Rate.Should().Be(60m);
        result.Value.FromCurrency.Should().Be("USD");
        result.Value.ToCurrency.Should().Be("VES");
    }

    [Fact]
    public async Task GetLatestRate_WithInverseRate_ShouldComputeInverse()
    {
        // Arrange: Solo existe USD -> VES a 50
        var directRate = new ExchangeRate(Currency.USD, Currency.VES, 50m, RateSource.BCV);
        _rateRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExchangeRate> { directRate });

        var handler = new GetLatestExchangeRateHandler(_rateRepoMock.Object, _mapperMock.Object);
        // Pedimos VES -> USD
        var query = new GetLatestExchangeRateQuery("VES", "USD");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Rate.Should().Be(0.02m); // 1 / 50 = 0.02
        result.Value.Source.Should().Contain("Inversa");
    }

    [Fact]
    public async Task ConvertCurrency_WithValidRate_ShouldConvertCorrectly()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetLatestExchangeRateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinTrack.Application.Common.Models.Result<ExchangeRateDto>.Ok(new ExchangeRateDto
            {
                FromCurrency = "USD",
                ToCurrency = "VES",
                Rate = 60m,
                Source = "BCV"
            }));

        var handler = new ConvertCurrencyHandler(mediatorMock.Object);
        var query = new ConvertCurrencyQuery(100m, "USD", "VES");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OriginalAmount.Should().Be(100m);
        result.Value.ConvertedAmount.Should().Be(6000m);
        result.Value.RateUsed.Should().Be(60m);
    }
}
