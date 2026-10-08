using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Reports.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinTrack.Infrastructure.Services;

public class ExportService : IExportService
{
    static ExportService()
    {
        // Configurar licencia comunitaria de QuestPDF para proyectos de código abierto
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateTransactionsExcelAsync(IEnumerable<TransactionExportItemDto> transactions, string currency)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Transacciones");

        // 1. Título y Metadatos
        worksheet.Cell(1, 1).Value = "FinTrack - Reporte de Transacciones";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1B5E20");

        worksheet.Cell(2, 1).Value = $"Exportado el: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC | Moneda base: {currency}";
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        worksheet.Cell(2, 1).Style.Font.FontSize = 10;
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        // 2. Encabezados de Tabla
        var headers = new[] { "Fecha", "Cuenta", "Tipo", "Categoría", "Descripción", $"Monto ({currency})", "Notas" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        worksheet.Row(4).Height = 24;

        // 3. Filas de Datos
        int row = 5;
        decimal totalIncome = 0;
        decimal totalExpense = 0;

        foreach (var t in transactions)
        {
            worksheet.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(row, 2).Value = t.AccountName;

            var typeCell = worksheet.Cell(row, 3);
            typeCell.Value = t.Type == "Income" ? "Ingreso (+)" : (t.Type == "Expense" ? "Gasto (-)" : t.Type);
            typeCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (t.Type == "Income")
            {
                typeCell.Style.Font.FontColor = XLColor.FromHtml("#2E7D32");
                totalIncome += t.Amount;
            }
            else if (t.Type == "Expense")
            {
                typeCell.Style.Font.FontColor = XLColor.FromHtml("#C62828");
                totalExpense += t.Amount;
            }

            worksheet.Cell(row, 4).Value = t.CategoryName;
            worksheet.Cell(row, 5).Value = t.Description;

            var amountCell = worksheet.Cell(row, 6);
            amountCell.Value = t.Amount;
            amountCell.Style.NumberFormat.Format = "#,##0.00";
            amountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(row, 7).Value = t.Notes;

            if (row % 2 == 0)
            {
                worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            }

            row++;
        }

        // 4. Resumen Total al Final
        row++;
        worksheet.Cell(row, 5).Value = "Total Ingresos:";
        worksheet.Cell(row, 5).Style.Font.Bold = true;
        worksheet.Cell(row, 6).Value = totalIncome;
        worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Cell(row, 6).Style.Font.Bold = true;
        worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");

        row++;
        worksheet.Cell(row, 5).Value = "Total Gastos:";
        worksheet.Cell(row, 5).Style.Font.Bold = true;
        worksheet.Cell(row, 6).Value = totalExpense;
        worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Cell(row, 6).Style.Font.Bold = true;
        worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.FromHtml("#C62828");

        row++;
        worksheet.Cell(row, 5).Value = "Balance Neto:";
        worksheet.Cell(row, 5).Style.Font.Bold = true;
        worksheet.Cell(row, 6).Value = totalIncome - totalExpense;
        worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Cell(row, 6).Style.Font.Bold = true;
        worksheet.Cell(row, 6).Style.Font.FontColor = (totalIncome - totalExpense >= 0) ? XLColor.FromHtml("#2E7D32") : XLColor.FromHtml("#C62828");

        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return Task.FromResult(memoryStream.ToArray());
    }

    public Task<byte[]> GenerateTransactionsCsvAsync(IEnumerable<TransactionExportItemDto> transactions)
    {
        var sb = new StringBuilder();
        // Encabezado CSV delimitado por punto y coma para Excel en español
        sb.AppendLine("Fecha;Cuenta;Tipo;Categoria;Descripcion;Monto;Moneda;Notas");

        foreach (var t in transactions)
        {
            var date = t.Date.ToString("yyyy-MM-dd HH:mm");
            var acc = EscapeCsv(t.AccountName);
            var type = t.Type == "Income" ? "Ingreso" : (t.Type == "Expense" ? "Gasto" : t.Type);
            var cat = EscapeCsv(t.CategoryName);
            var desc = EscapeCsv(t.Description);
            var amount = t.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            var currency = t.Currency;
            var notes = EscapeCsv(t.Notes);

            sb.AppendLine($"{date};{acc};{type};{cat};{desc};{amount};{currency};{notes}");
        }

        // Emitir UTF-8 con BOM para que Excel en Windows reconozca caracteres y acentos correctamente
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var preamble = encoding.GetPreamble();
        var contentBytes = encoding.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, result, preamble.Length, contentBytes.Length);
        return Task.FromResult(result);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    public Task<byte[]> GenerateFinancialReportExcelAsync(FinancialReportExportDataDto reportData)
    {
        using var workbook = new XLWorkbook();

        // Pestaña 1: Resumen Ejecutivo
        var wsSummary = workbook.Worksheets.Add("Resumen Ejecutivo");
        wsSummary.Cell(1, 1).Value = "FinTrack - Estado Financiero";
        wsSummary.Cell(1, 1).Style.Font.Bold = true;
        wsSummary.Cell(1, 1).Style.Font.FontSize = 16;
        wsSummary.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1B5E20");

        wsSummary.Cell(2, 1).Value = $"Período: {reportData.PeriodTitle} | Emitido: {reportData.GeneratedAt:dd/MM/yyyy HH:mm} UTC";
        wsSummary.Cell(2, 1).Style.Font.Italic = true;

        // KPIs
        wsSummary.Cell(4, 1).Value = "Métrica Financiera";
        wsSummary.Cell(4, 2).Value = $"Valor ({reportData.BaseCurrency})";
        wsSummary.Range(4, 1, 4, 2).Style.Font.Bold = true;
        wsSummary.Range(4, 1, 4, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
        wsSummary.Range(4, 1, 4, 2).Style.Font.FontColor = XLColor.White;

        wsSummary.Cell(5, 1).Value = "Total Ingresos";
        wsSummary.Cell(5, 2).Value = reportData.TotalIncome;
        wsSummary.Cell(5, 2).Style.NumberFormat.Format = "#,##0.00";

        wsSummary.Cell(6, 1).Value = "Total Gastos";
        wsSummary.Cell(6, 2).Value = reportData.TotalExpense;
        wsSummary.Cell(6, 2).Style.NumberFormat.Format = "#,##0.00";

        wsSummary.Cell(7, 1).Value = "Ahorro Neto";
        wsSummary.Cell(7, 2).Value = reportData.NetSavings;
        wsSummary.Cell(7, 2).Style.NumberFormat.Format = "#,##0.00";

        wsSummary.Cell(8, 1).Value = "Tasa de Ahorro";
        wsSummary.Cell(8, 2).Value = $"{reportData.SavingsRate:N1}%";

        wsSummary.Cell(9, 1).Value = "Tasa Oficial BCV USD";
        wsSummary.Cell(9, 2).Value = $"Bs. {reportData.UsdExchangeRate:N2}";

        wsSummary.Cell(10, 1).Value = "Tasa Oficial BCV EUR";
        wsSummary.Cell(10, 2).Value = $"Bs. {reportData.EurExchangeRate:N2}";

        // Categorías
        wsSummary.Cell(12, 1).Value = "Desglose de Gastos por Categoría";
        wsSummary.Cell(12, 1).Style.Font.Bold = true;
        wsSummary.Cell(12, 1).Style.Font.FontSize = 13;

        wsSummary.Cell(13, 1).Value = "Categoría";
        wsSummary.Cell(13, 2).Value = "Monto";
        wsSummary.Cell(13, 3).Value = "% del Total";
        wsSummary.Range(13, 1, 13, 3).Style.Font.Bold = true;
        wsSummary.Range(13, 1, 13, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
        wsSummary.Range(13, 1, 13, 3).Style.Font.FontColor = XLColor.White;

        int catRow = 14;
        foreach (var c in reportData.Categories)
        {
            wsSummary.Cell(catRow, 1).Value = c.CategoryName;
            wsSummary.Cell(catRow, 2).Value = c.Amount;
            wsSummary.Cell(catRow, 2).Style.NumberFormat.Format = "#,##0.00";
            wsSummary.Cell(catRow, 3).Value = $"{c.Percentage:N1}%";
            catRow++;
        }

        wsSummary.Columns().AdjustToContents();

        // Pestaña 2: Movimientos del período
        var wsTx = workbook.Worksheets.Add("Movimientos");
        var txHeaders = new[] { "Fecha", "Cuenta", "Tipo", "Categoría", "Descripción", $"Monto ({reportData.BaseCurrency})", "Notas" };
        for (int i = 0; i < txHeaders.Length; i++)
        {
            var cell = wsTx.Cell(1, i + 1);
            cell.Value = txHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
        }

        int txRow = 2;
        foreach (var t in reportData.Transactions)
        {
            wsTx.Cell(txRow, 1).Value = t.Date.ToString("yyyy-MM-dd HH:mm");
            wsTx.Cell(txRow, 2).Value = t.AccountName;
            wsTx.Cell(txRow, 3).Value = t.Type == "Income" ? "Ingreso" : (t.Type == "Expense" ? "Gasto" : t.Type);
            wsTx.Cell(txRow, 4).Value = t.CategoryName;
            wsTx.Cell(txRow, 5).Value = t.Description;
            wsTx.Cell(txRow, 6).Value = t.Amount;
            wsTx.Cell(txRow, 6).Style.NumberFormat.Format = "#,##0.00";
            wsTx.Cell(txRow, 7).Value = t.Notes;
            txRow++;
        }
        wsTx.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public Task<byte[]> GenerateFinancialReportPdfAsync(FinancialReportExportDataDto reportData)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                // 1. Cabecera
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text("FinTrack").FontSize(20).Bold().FontColor(Colors.Green.Darken2);
                            titleCol.Item().Text("Estado de Cuenta & Reporte Financiero").FontSize(13).SemiBold();
                            titleCol.Item().Text($"Período: {reportData.PeriodTitle}").FontSize(10).FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(180).Column(metaCol =>
                        {
                            metaCol.Item().AlignRight().Text($"Emisión: {reportData.GeneratedAt:dd/MM/yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                            metaCol.Item().AlignRight().Text($"Moneda Base: {reportData.BaseCurrency}").FontSize(9).Bold();
                            metaCol.Item().AlignRight().Text($"BCV USD: Bs. {reportData.UsdExchangeRate:N2}").FontSize(8).FontColor(Colors.Blue.Darken2);
                            metaCol.Item().AlignRight().Text($"BCV EUR: Bs. {reportData.EurExchangeRate:N2}").FontSize(8).FontColor(Colors.Blue.Darken2);
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // 2. Contenido Principal
                page.Content().PaddingTop(14).Column(col =>
                {
                    // Tarjetas de Resumen Ejecutivo (KPIs)
                    col.Item().Row(row =>
                    {
                        // Ingresos
                        row.RelativeItem().PaddingRight(4).Border(1).BorderColor(Colors.Green.Lighten3).Background(Colors.Green.Lighten5).Padding(8).Column(c =>
                        {
                            c.Item().Text("Ingresos Totales").FontSize(8).FontColor(Colors.Green.Darken3);
                            c.Item().Text($"+{reportData.TotalIncome:N2}").FontSize(13).Bold().FontColor(Colors.Green.Darken2);
                        });

                        // Gastos
                        row.RelativeItem().PaddingHorizontal(2).Border(1).BorderColor(Colors.Red.Lighten3).Background(Colors.Red.Lighten5).Padding(8).Column(c =>
                        {
                            c.Item().Text("Gastos Totales").FontSize(8).FontColor(Colors.Red.Darken3);
                            c.Item().Text($"-{reportData.TotalExpense:N2}").FontSize(13).Bold().FontColor(Colors.Red.Darken2);
                        });

                        // Ahorro Neto
                        row.RelativeItem().PaddingHorizontal(2).Border(1).BorderColor(Colors.Blue.Lighten3).Background(Colors.Blue.Lighten5).Padding(8).Column(c =>
                        {
                            c.Item().Text("Superávit / Ahorro Neto").FontSize(8).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"{reportData.NetSavings:N2}").FontSize(13).Bold().FontColor(reportData.NetSavings >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        });

                        // Tasa de Ahorro
                        row.RelativeItem().PaddingLeft(4).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("Tasa de Ahorro").FontSize(8).FontColor(Colors.Grey.Darken2);
                            c.Item().Text($"{reportData.SavingsRate:N1}%").FontSize(13).Bold().FontColor(Colors.Grey.Darken3);
                        });
                    });

                    // Sección: Gastos por Categoría
                    col.Item().PaddingTop(16).Text("Distribución de Gastos por Categoría").FontSize(11).Bold().FontColor(Colors.Grey.Darken4);
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Categoría").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Monto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Porcentaje").Bold();
                        });

                        if (!reportData.Categories.Any())
                        {
                            table.Cell().ColumnSpan(3).Padding(8).Text("No se registran gastos en este período.").Italic().FontColor(Colors.Grey.Medium);
                        }
                        else
                        {
                            foreach (var cat in reportData.Categories)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(cat.CategoryName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{cat.Amount:N2}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{cat.Percentage:N1}%");
                            }
                        }
                    });

                    // Sección: Movimientos Destacados
                    col.Item().PaddingTop(16).Text("Movimientos del Período").FontSize(11).Bold().FontColor(Colors.Grey.Darken4);
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(75);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("Fecha").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("Cuenta").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("Categoría").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("Descripción").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Green.Darken2).Padding(5).AlignRight().Text("Monto").FontColor(Colors.White).Bold();
                        });

                        if (!reportData.Transactions.Any())
                        {
                            table.Cell().ColumnSpan(5).Padding(8).Text("No hay transacciones registradas para este período.").Italic().FontColor(Colors.Grey.Medium);
                        }
                        else
                        {
                            foreach (var tx in reportData.Transactions)
                            {
                                var isIncome = tx.Type == "Income";
                                var color = isIncome ? Colors.Green.Darken2 : Colors.Red.Darken2;
                                var sign = isIncome ? "+" : "-";

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(tx.Date.ToString("dd/MM/yyyy")).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(tx.AccountName).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(tx.CategoryName).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(tx.Description).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).AlignRight().Text($"{sign}{tx.Amount:N2}").Bold().FontColor(color).FontSize(8);
                            }
                        }
                    });
                });

                // 3. Pie de Página
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text("FinTrack - Gestión Financiera Inteligente & Multi-Moneda").FontSize(7).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Página ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                    });
                });
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return Task.FromResult(ms.ToArray());
    }
}
