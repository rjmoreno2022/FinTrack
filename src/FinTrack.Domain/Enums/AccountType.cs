namespace FinTrack.Domain.Enums;

/// <summary>
/// Representa el tipo de cuenta financiera.
/// </summary>
public enum AccountType
{
    Bank = 1,        // Cuenta bancaria
    Cash = 2,        // Efectivo físico
    Crypto = 3,      // Exchange crypto
    Wallet = 4,      // Billetera digital
    Savings = 5      // Cuenta de ahorro dedicada
}
