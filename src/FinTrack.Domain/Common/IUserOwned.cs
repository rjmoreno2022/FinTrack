using System;

namespace FinTrack.Domain.Common;

/// <summary>
/// Contrato para entidades que pertenecen a un usuario específico en el sistema multi-usuario.
/// </summary>
public interface IUserOwned
{
    Guid UserId { get; }
    void AssignOwner(Guid userId);
}
