using System;

namespace FinTrack.Application.Accounts.DTOs;

public class AccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal Balance { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Bank";
    public string Currency { get; set; } = "USD";
    public decimal InitialBalance { get; set; } = 0;
    public string? Description { get; set; }
}

public class UpdateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Balance { get; set; }
}

public class TransferFundsRequest
{
    public Guid SourceAccountId { get; set; }
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal? DestinationAmount { get; set; }
    public DateTime? Date { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Tags { get; set; }
}
