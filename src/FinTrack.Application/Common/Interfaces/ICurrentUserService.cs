using System;

namespace FinTrack.Application.Common.Interfaces;

/// <summary>
/// Proporciona la identidad del usuario actual autenticado en el contexto de ejecución.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
