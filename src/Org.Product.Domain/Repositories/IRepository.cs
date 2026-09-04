using Org.Product.Domain.Entities.Base;

namespace Org.Product.Domain.Repositories
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
