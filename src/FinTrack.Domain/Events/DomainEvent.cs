using System;

namespace FinTrack.Domain.Events;

/// <summary>
/// Clase base para todos los eventos de dominio.
/// </summary>
public abstract class DomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
