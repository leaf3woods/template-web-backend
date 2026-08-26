using Microsoft.EntityFrameworkCore;

namespace Template.Web.Application.Persistence;

/// <summary>
///     Defines the persistence capabilities required by application use cases.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<TEntity> Set<TEntity>()
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IApplicationTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default
    );

    Task<int> ExcuteSqlRaw(string sql, CancellationToken cancellationToken = default);
}
