namespace FinTrack.Domain.Enums;

/// <summary>
/// Representa el estado de una meta de ahorro.
/// </summary>
public enum GoalStatus
{
    Active = 1,      // Activa, acumulando fondos
    Completed = 2,   // Meta alcanzada
    Cancelled = 3,   // Cancelada
    Paused = 4       // Pausada temporalmente
}
