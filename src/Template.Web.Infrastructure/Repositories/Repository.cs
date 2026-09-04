using Microsoft.EntityFrameworkCore;
using Template.Web.Domain.Entities.Base;
using Template.Web.Domain.Repositories;

namespace Template.Web.Infrastructure.Repositories
{
    public sealed class Repository<TEntity> : IRepository<TEntity>
        where TEntity : class, IAggregateRoot
    {
        private readonly ApiDbContext _dbContext;

        public Repository(ApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<TEntity> Query(bool tracking = true)
        {
            var query = _dbContext.Set<TEntity>().AsQueryable();
            return tracking ? query : query.AsNoTracking();
        }

        public async Task<TEntity?> FindAsync(params object?[]? ids)
        {
            return await _dbContext.Set<TEntity>().FindAsync(ids);
        }

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        }

        public void Update(TEntity entity)
        {
            _dbContext.Set<TEntity>().Update(entity);
        }

        public void Remove(TEntity entity)
        {
            _dbContext.Set<TEntity>().Remove(entity);
        }
    }
}
