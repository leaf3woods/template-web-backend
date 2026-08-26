using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Template.Web.Domain.Entities.Base;

namespace Template.Web.Domain.Repositories
{
    public interface IRepository<TEntity> where TEntity : AggregateRoot
    {
        IQueryable<TEntity> All(params Expression<Func<TEntity, object>>[] includeExpressions);
    }
}
