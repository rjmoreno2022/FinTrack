namespace FinTrack.Domain.Enums;

/// <summary>
/// Indica si una transacción es un ingreso, un gasto o una transferencia interna.
/// </summary>
public enum TransactionType
{
    Income = 1,      // Ingreso (salario, freelance, regalo, etc)
    Expense = 2,     // Gasto (comida, alquiler, servicios, etc)
    Transfer = 3     // Transferencia entre cuentas propias
}
