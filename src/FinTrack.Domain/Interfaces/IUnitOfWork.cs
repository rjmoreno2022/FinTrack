using System.Threading;
using System.Threading.Tasks;

namespace FinTrack.Domain.Interfaces;

/// <summary>
/// Contrato para el patrón Unit of Work, asegurando transaccionalidad atómica en los casos de uso.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
