using System;

namespace FinTrack.Application.Transactions.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string? AccountName { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Tags { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? TransferAccountId { get; set; }
    public Guid? RelatedTransactionId { get; set; }
}

public class CreateTransactionRequest
{
    public Guid AccountId { get; set; }
    public string Type { get; set; } = "Expense";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public Guid? CategoryId { get; set; }
    public string? Tags { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? TransferAccountId { get; set; }
}

public class UpdateTransactionRequest
{
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public Guid? CategoryId { get; set; }
    public string? Tags { get; set; }
}

