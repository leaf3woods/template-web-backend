using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Org.Product.Domain.Entities.Base;
using Org.Product.Domain.Shared;

namespace Org.Product.Application.Utilities;

public static class QueryableExtensions
{
    public static IQueryable<TEntity> WhereIf<TEntity>(
        this IQueryable<TEntity> queryable,
        bool condition,
        Expression<Func<TEntity, bool>> predicate
    )
        where TEntity : IAggregateRoot => condition ? queryable.Where(predicate) : queryable;

    public static async Task<PaginatedList<TEntity>> ToPaginatedListAsync<TEntity>(
        this IQueryable<TEntity> entities,
        int pageIndex,
        int pageSize
    )
    {
        var count = await entities.LongCountAsync();
        var query = await entities.Skip(pageIndex * pageSize).Take(pageSize).ToArrayAsync();
        return new PaginatedList<TEntity>(query, count, pageIndex, pageSize);
    }
}
