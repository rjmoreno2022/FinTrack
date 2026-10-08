using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Reports.DTOs;
using FinTrack.Application.Reports.Queries;
using FinTrack.Application.Transactions.Queries;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Reports;

public class ExportHandlerTests
{
    private readonly Mock<IRepository<Account>> _accountRepoMock;
    private readonly Mock<IRepository<Category>> _categoryRepoMock;
    private readonly Mock<IRepository<ExchangeRate>> _exchangeRateRepoMock;
    private readonly Mock<IExportService> _exportServiceMock;

    public ExportHandlerTests()
    {
        _accountRepoMock = new Mock<IRepository<Account>>();
        _categoryRepoMock = new Mock<IRepository<Category>>();
        _exchangeRateRepoMock = new Mock<IRepository<ExchangeRate>>();
        _exportServiceMock = new Mock<IExportService>();

        _accountRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());
        _categoryRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());
        _exchangeRateRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExchangeRate>());
    }

    [Fact]
    public async Task ExportTransactions_WhenFormatIsExcel_ShouldReturnExcelFileResult()
    {
        // Arrange
        var account = new Account("Banesco Panamá", AccountType.Bank, Currency.USD);
        account.AddTransaction(TransactionType.Income, 1500m, "Pago de nómina", DateTime.UtcNow);
        account.AddTransaction(TransactionType.Expense, 200m, "Supermercado", DateTime.UtcNow);

        _accountRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });
        _categoryRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var dummyExcelBytes = Encoding.UTF8.GetBytes("PK_MOCK_EXCEL_BYTES");
        _exportServiceMock.Setup(s => s.GenerateTransactionsExcelAsync(It.IsAny<IEnumerable<TransactionExportItemDto>>(), "USD"))
            .ReturnsAsync(dummyExcelBytes);

        var handler = new ExportTransactionsQueryHandler(_accountRepoMock.Object, _categoryRepoMock.Object, _exportServiceMock.Object);
        var query = new ExportTransactionsQuery(Format: "xlsx", Currency: "USD");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FileName.Should().EndWith(".xlsx");
        result.Value.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.Content.Should().BeEquivalentTo(dummyExcelBytes);
        _exportServiceMock.Verify(s => s.GenerateTransactionsExcelAsync(It.Is<IEnumerable<TransactionExportItemDto>>(items => items.Count() == 2), "USD"), Times.Once);
    }

    [Fact]
    public async Task ExportTransactions_WhenFormatIsCsv_ShouldReturnCsvFileResult()
    {
        // Arrange
        var account = new Account("Efectivo USD", AccountType.Cash, Currency.USD);
        account.AddTransaction(TransactionType.Expense, 50m, "Almuerzo", DateTime.UtcNow);

        _accountRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });
        _categoryRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var dummyCsvBytes = Encoding.UTF8.GetBytes("Fecha;Cuenta;Tipo;Categoria;Descripcion;Monto;Moneda;Notas");
        _exportServiceMock.Setup(s => s.GenerateTransactionsCsvAsync(It.IsAny<IEnumerable<TransactionExportItemDto>>()))
            .ReturnsAsync(dummyCsvBytes);

        var handler = new ExportTransactionsQueryHandler(_accountRepoMock.Object, _categoryRepoMock.Object, _exportServiceMock.Object);
        var query = new ExportTransactionsQuery(Format: "csv", Currency: "USD");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FileName.Should().EndWith(".csv");
        result.Value.ContentType.Should().Be("text/csv; charset=utf-8");
        result.Value.Content.Should().BeEquivalentTo(dummyCsvBytes);
        _exportServiceMock.Verify(s => s.GenerateTransactionsCsvAsync(It.Is<IEnumerable<TransactionExportItemDto>>(items => items.Count() == 1)), Times.Once);
    }

    [Fact]
    public async Task ExportFinancialReportPdf_ShouldBuildReportDataAndReturnPdfFileResult()
    {
        // Arrange
        var account = new Account("Banesco USD", AccountType.Bank, Currency.USD);
        account.AddTransaction(TransactionType.Income, 2000m, "Salario", DateTime.UtcNow);
        account.AddTransaction(TransactionType.Expense, 500m, "Servicios", DateTime.UtcNow);

        _accountRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });
        _categoryRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var usdRate = new ExchangeRate(Currency.USD, Currency.VES, 842.25m, RateSource.BCV);
        var eurRate = new ExchangeRate(Currency.EUR, Currency.VES, 977.50m, RateSource.BCV);
        _exchangeRateRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExchangeRate> { usdRate, eurRate });

        var dummyPdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 MOCK");
        _exportServiceMock.Setup(s => s.GenerateFinancialReportPdfAsync(It.IsAny<FinancialReportExportDataDto>()))
            .ReturnsAsync(dummyPdfBytes);

        var handler = new ExportFinancialReportPdfQueryHandler(
            _accountRepoMock.Object,
            _categoryRepoMock.Object,
            _exchangeRateRepoMock.Object,
            _exportServiceMock.Object
        );

        var query = new ExportFinancialReportPdfQuery(Period: "CurrentMonth", Currency: "USD");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FileName.Should().EndWith(".pdf");
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.Content.Should().BeEquivalentTo(dummyPdfBytes);

        _exportServiceMock.Verify(s => s.GenerateFinancialReportPdfAsync(It.Is<FinancialReportExportDataDto>(d =>
            d.TotalIncome == 2000m &&
            d.TotalExpense == 500m &&
            d.NetSavings == 1500m &&
            d.SavingsRate == 75.0m &&
            d.UsdExchangeRate == 842.25m &&
            d.EurExchangeRate == 977.50m
        )), Times.Once);
    }

    [Fact]
    public async Task ExportFinancialReportExcel_ShouldBuildReportDataAndReturnExcelFileResult()
    {
        // Arrange
        var account = new Account("Banesco USD", AccountType.Bank, Currency.USD);
        account.AddTransaction(TransactionType.Income, 1000m, "Ingreso", DateTime.UtcNow);
        account.AddTransaction(TransactionType.Expense, 400m, "Gasto", DateTime.UtcNow);

        _accountRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });
        _categoryRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var usdRate = new ExchangeRate(Currency.USD, Currency.VES, 842.25m, RateSource.BCV);
        var eurRate = new ExchangeRate(Currency.EUR, Currency.VES, 977.50m, RateSource.BCV);
        _exchangeRateRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExchangeRate> { usdRate, eurRate });

        var dummyExcelBytes = Encoding.UTF8.GetBytes("PK_MOCK_EXCEL_BYTES");
        _exportServiceMock.Setup(s => s.GenerateFinancialReportExcelAsync(It.IsAny<FinancialReportExportDataDto>()))
            .ReturnsAsync(dummyExcelBytes);

        var handler = new ExportFinancialReportExcelQueryHandler(
            _accountRepoMock.Object,
            _categoryRepoMock.Object,
            _exchangeRateRepoMock.Object,
            _exportServiceMock.Object
        );

        var query = new ExportFinancialReportExcelQuery(Period: "CurrentMonth", Currency: "USD");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FileName.Should().EndWith(".xlsx");
        result.Value.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.Content.Should().BeEquivalentTo(dummyExcelBytes);

        _exportServiceMock.Verify(s => s.GenerateFinancialReportExcelAsync(It.Is<FinancialReportExportDataDto>(d =>
            d.TotalIncome == 1000m &&
            d.TotalExpense == 400m &&
            d.NetSavings == 600m &&
            d.SavingsRate == 60.0m
        )), Times.Once);
    }
}
