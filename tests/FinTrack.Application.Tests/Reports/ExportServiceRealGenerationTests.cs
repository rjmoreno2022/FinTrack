using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FinTrack.Application.Reports.DTOs;
using FinTrack.Infrastructure.Services;
using Xunit;

namespace FinTrack.Application.Tests.Reports;

public class ExportServiceRealGenerationTests
{
    private readonly ExportService _exportService;

    public ExportServiceRealGenerationTests()
    {
        _exportService = new ExportService();
    }

    [Fact]
    public async Task GenerateTransactionsExcelAsync_ShouldGenerateValidZipExcelBytes()
    {
        // Arrange
        var items = new List<TransactionExportItemDto>
        {
            new(Guid.NewGuid(), DateTime.UtcNow, "Pago Freelance", "Income", 1250.50m, "USD", "Banesco", "Trabajo", "Honorarios"),
            new(Guid.NewGuid(), DateTime.UtcNow, "Supermercado", "Expense", 85.20m, "USD", "Efectivo", "Alimentación", "Semanal")
        };

        // Act
        var bytes = await _exportService.GenerateTransactionsExcelAsync(items, "USD");

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(100);
        // ZIP magic bytes for .xlsx: PK\x03\x04 (0x50, 0x4B, 0x03, 0x04)
        bytes[0].Should().Be(0x50);
        bytes[1].Should().Be(0x4B);
    }

    [Fact]
    public async Task GenerateTransactionsCsvAsync_ShouldGenerateUtf8BomAndSemicolonDelimitedCsv()
    {
        // Arrange
        var items = new List<TransactionExportItemDto>
        {
            new(Guid.NewGuid(), new DateTime(2026, 9, 15, 10, 30, 0), "Almuerzo Ejecutivo", "Expense", 15.00m, "USD", "Zelle", "Restaurantes", "Con colegas")
        };

        // Act
        var bytes = await _exportService.GenerateTransactionsCsvAsync(items);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
        // UTF-8 BOM: 0xEF, 0xBB, 0xBF
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        var csvText = Encoding.UTF8.GetString(bytes);
        csvText.Should().Contain("Fecha;Cuenta;Tipo;Categoria;Descripcion;Monto;Moneda;Notas");
        csvText.Should().Contain("Almuerzo Ejecutivo");
        csvText.Should().Contain("15.00");
    }

    [Fact]
    public async Task GenerateFinancialReportPdfAsync_ShouldGenerateValidPdfDocument()
    {
        // Arrange
        var reportData = new FinancialReportExportDataDto(
            PeriodTitle: "Mes de Septiembre 2026",
            GeneratedAt: DateTime.UtcNow,
            BaseCurrency: "USD",
            TotalIncome: 3500m,
            TotalExpense: 1200m,
            NetSavings: 2300m,
            SavingsRate: 65.7m,
            UsdExchangeRate: 842.21m,
            EurExchangeRate: 977.88m,
            Categories: new List<CategoryExpenseReportItemDto>
            {
                new("Alimentación", 600m, 50.0m),
                new("Servicios", 400m, 33.3m),
                new("Transporte", 200m, 16.7m)
            },
            Transactions: new List<TransactionExportItemDto>
            {
                new(Guid.NewGuid(), DateTime.UtcNow, "Nómina", "Income", 3500m, "USD", "Banesco", "Ingresos", ""),
                new(Guid.NewGuid(), DateTime.UtcNow, "Mercado", "Expense", 600m, "USD", "Banesco", "Alimentación", "")
            }
        );

        // Act
        var bytes = await _exportService.GenerateFinancialReportPdfAsync(reportData);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(200);
        var header = Encoding.ASCII.GetString(bytes, 0, 5);
        header.Should().Be("%PDF-");
    }

    [Fact]
    public async Task GenerateFinancialReportExcelAsync_ShouldGenerateValidMultiTabExcelBytes()
    {
        // Arrange
        var reportData = new FinancialReportExportDataDto(
            PeriodTitle: "Mes de Septiembre 2026",
            GeneratedAt: DateTime.UtcNow,
            BaseCurrency: "USD",
            TotalIncome: 2500m,
            TotalExpense: 1000m,
            NetSavings: 1500m,
            SavingsRate: 60.0m,
            UsdExchangeRate: 842.21m,
            EurExchangeRate: 977.88m,
            Categories: new List<CategoryExpenseReportItemDto>
            {
                new("Hogar", 1000m, 100.0m)
            },
            Transactions: new List<TransactionExportItemDto>
            {
                new(Guid.NewGuid(), DateTime.UtcNow, "Pago Alquiler", "Expense", 1000m, "USD", "Banesco", "Hogar", "")
            }
        );

        // Act
        var bytes = await _exportService.GenerateFinancialReportExcelAsync(reportData);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(100);
        // ZIP magic bytes: PK\x03\x04
        bytes[0].Should().Be(0x50);
        bytes[1].Should().Be(0x4B);
    }
}
