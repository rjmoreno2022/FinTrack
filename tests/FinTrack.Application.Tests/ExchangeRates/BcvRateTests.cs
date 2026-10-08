using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.ExchangeRates.Commands;
using FinTrack.Application.ExchangeRates.DTOs;
using FinTrack.Application.ExchangeRates.Queries;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.ExchangeRates;

public class BcvRateTests
{
    [Fact]
    public async Task GetBcvLiveRateQueryHandler_ShouldReturnSuccess_WhenProviderReturnsValidRate()
    {
        // Arrange
        var mockProvider = new Mock<IBcvRateProvider>();
        var expectedDto = new BcvRateResultDto(
            Rate: 842.21m,
            EffectiveDate: DateTime.UtcNow,
            Source: "BCV (Oficial)",
            IsDirectPortal: true
        );

        mockProvider
            .Setup(p => p.GetLiveRateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var handler = new GetBcvLiveRateQueryHandler(mockProvider.Object);

        // Act
        var result = await handler.Handle(new GetBcvLiveRateQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(842.21m, result.Value.Rate);
        Assert.Equal("BCV (Oficial)", result.Value.Source);
        Assert.True(result.Value.IsDirectPortal);
    }

    [Fact]
    public async Task GetLiveOfficialRatesQueryHandler_ShouldReturnBothUsdAndEur_WhenValid()
    {
        // Arrange
        var mockProvider = new Mock<IBcvRateProvider>();
        var expectedDto = new OfficialRatesDto(
            UsdRate: 842.21m,
            EurRate: 977.88m,
            EffectiveDate: DateTime.UtcNow,
            Source: "BCV (Oficial)",
            IsDirectPortal: true
        );

        mockProvider
            .Setup(p => p.GetLiveOfficialRatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var handler = new GetLiveOfficialRatesQueryHandler(mockProvider.Object);

        // Act
        var result = await handler.Handle(new GetLiveOfficialRatesQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(842.21m, result.Value.UsdRate);
        Assert.Equal(977.88m, result.Value.EurRate);
    }

    [Fact]
    public async Task SyncOfficialRatesCommandHandler_ShouldDeactivatePriorAndPersistBothUsdAndEur()
    {
        // Arrange
        var mockProvider = new Mock<IBcvRateProvider>();
        var officialDto = new OfficialRatesDto(
            UsdRate: 842.21m,
            EurRate: 977.88m,
            EffectiveDate: DateTime.UtcNow,
            Source: "BCV (Oficial)",
            IsDirectPortal: true
        );

        mockProvider
            .Setup(p => p.GetLiveOfficialRatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(officialDto);

        var existingRates = new List<ExchangeRate>
        {
            new ExchangeRate(Currency.USD, Currency.VES, 830.0m, RateSource.BCV),
            new ExchangeRate(Currency.EUR, Currency.VES, 960.0m, RateSource.BCV)
        };

        var mockRepo = new Mock<IRepository<ExchangeRate>>();
        mockRepo
            .Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRates);

        var mockUow = new Mock<IUnitOfWork>();

        var handler = new SyncOfficialRatesCommandHandler(mockProvider.Object, mockRepo.Object, mockUow.Object);

        // Act
        var result = await handler.Handle(new SyncOfficialRatesCommand(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(842.21m, result.Value.UsdRate);
        Assert.Equal(977.88m, result.Value.EurRate);

        // Verify old rates were deactivated
        Assert.All(existingRates, r => Assert.False(r.IsActive));

        // Verify two new rates were added (USD and EUR)
        mockRepo.Verify(r => r.AddAsync(It.IsAny<ExchangeRate>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
