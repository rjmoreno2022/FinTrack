namespace FinTrack.Domain.Enums;

/// <summary>
/// Representa el estado actual de una deuda.
/// </summary>
public enum DebtStatus
{
    Active = 1,      // Pendiente de pago
    Paid = 2,        // Pagada completamente
    Defaulted = 3,   // Incumplida / En mora
    Cancelled = 4    // Cancelada o perdonada por acuerdo
}
