using System;

namespace FinTrack.Application.Reports.DTOs;

public record TransactionExportItemDto(
    Guid Id,
    DateTime Date,
    string Description,
    string Type,
    decimal Amount,
    string Currency,
    string AccountName,
    string CategoryName,
    string Notes
);
