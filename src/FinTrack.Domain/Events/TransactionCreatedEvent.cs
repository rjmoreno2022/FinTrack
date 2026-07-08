using System;
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Events;

public class TransactionCreatedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public Guid AccountId { get; }
    public decimal Amount { get; }
    public TransactionType Type { get; }

    public TransactionCreatedEvent(Guid transactionId, Guid accountId, decimal amount, TransactionType type)
    {
        TransactionId = transactionId;
        AccountId = accountId;
        Amount = amount;
        Type = type;
    }
}
