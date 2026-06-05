using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using StackExchange.Redis;
using Template.Web.Domain.Entities.Base;

namespace Template.Web.Infrastructure.DbContexts
{
    public static class SoftDeleteQueryExtension
    {
        public static void AddSoftDeleteQueryFilter(this IMutableEntityType entityData)
        {
            var methodToCall = typeof(SoftDeleteQueryExtension)
                .GetMethod(
                    nameof(GetSoftDeleteFilter),
                    BindingFlags.NonPublic | BindingFlags.Static
                )!
                .MakeGenericMethod(entityData.ClrType);
            var filter = methodToCall.Invoke(null, []);
            entityData.SetQueryFilter((LambdaExpression)filter!);
        }

        private static Expression<Func<TEntity, bool>> GetSoftDeleteFilter<TEntity>()
            where TEntity : class, ISoftDelete => x => !x.SoftDeleted;

        public static IQueryable<T> WhereIf<T>(
            this IQueryable<T> queryable,
            bool condition,
            Expression<Func<T, bool>> predicate
        )
            where T : AggregateRoot
        {
            if (condition)
                return queryable.Where(predicate);
            else
                return queryable;
        }
    }
}
