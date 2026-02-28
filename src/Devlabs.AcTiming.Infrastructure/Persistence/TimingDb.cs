using Devlabs.AcTiming.Application.Abstractions;
using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Devlabs.AcTiming.Infrastructure.Persistence;

internal class TimingDb(AcTimingDbContext dbContext) : ITimingDb
{
    public IQueryable<TEntity> AsNoTracking<TEntity>()
        where TEntity : Entity
    {
        return dbContext.Set<TEntity>().AsNoTracking();
    }

    public IQueryable<TEntity> AsTracking<TEntity>()
        where TEntity : Entity
    {
        return dbContext.Set<TEntity>();
    }

    public void Insert<TEntity>(TEntity entity)
        where TEntity : Entity
    {
        dbContext.Add(entity);
    }

    public void Insert<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : Entity
    {
        dbContext.AddRange(entities);
    }

    public void Delete<TEntity>(TEntity entity)
        where TEntity : Entity
    {
        dbContext.Remove(entity);
    }

    public void Delete<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : Entity
    {
        dbContext.RemoveRange(entities);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return dbContext.SaveChangesAsync(ct);
    }
}
