using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Domain.Interfaces;

namespace FinTrack.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly FinTrackDbContext _dbContext;

    public UnitOfWork(FinTrackDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
