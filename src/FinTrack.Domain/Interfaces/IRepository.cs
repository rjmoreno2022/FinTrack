using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Domain.Common;

namespace FinTrack.Domain.Interfaces;

/// <summary>
/// Contrato genérico para operaciones de persistencia (repositorios) de raíces de agregado.
/// </summary>
/// <typeparam name="T">El tipo de la entidad raíz del agregado.</typeparam>
public interface IRepository<T> where T : Entity, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}
