using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Application.Abstractions;

public interface ITimingDb
{
    IQueryable<TEntity> AsNoTracking<TEntity>()
        where TEntity : Entity;
    IQueryable<TEntity> AsTracking<TEntity>()
        where TEntity : Entity;

    void Insert<TEntity>(TEntity entity)
        where TEntity : Entity;

    void Insert<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : Entity;

    void Delete<TEntity>(TEntity entity)
        where TEntity : Entity;

    void Delete<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : Entity;

    Task SaveChangesAsync(CancellationToken ct = default);
}
