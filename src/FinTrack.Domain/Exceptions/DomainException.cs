using System;

namespace FinTrack.Domain.Exceptions;

/// <summary>
/// Excepción personalizada para representar violaciones a las reglas del negocio en la capa de dominio.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
