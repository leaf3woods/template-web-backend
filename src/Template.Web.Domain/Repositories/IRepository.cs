using Template.Web.Domain.Entities.Base;

namespace Template.Web.Domain.Repositories
{
    public interface IRepository<TEntity>
        where TEntity : IAggregateRoot
    {
        IQueryable<TEntity> Query(bool tracking = true);

        Task<TEntity?> FindAsync(params object?[]? ids);

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        void Update(TEntity entity);

        void Remove(TEntity entity);
    }
}
