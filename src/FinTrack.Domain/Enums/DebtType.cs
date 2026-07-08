namespace FinTrack.Domain.Enums;

/// <summary>
/// Indica si es dinero que debemos (Owed) o dinero que nos deben (Receivable).
/// </summary>
public enum DebtType
{
    Owed = 1,        // Debo dinero (ej: Cashea, tarjeta de crédito)
    Receivable = 2   // Me deben dinero
}
