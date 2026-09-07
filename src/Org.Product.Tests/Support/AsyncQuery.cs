using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Org.Product.Tests.Support;

internal sealed class AsyncQuery<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public AsyncQuery(IEnumerable<T> source) : base(source) { }

    public AsyncQuery(Expression expression) : base(expression) { }

    IQueryProvider IQueryable.Provider => new AsyncQueryProvider(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new AsyncEnumerator(this.AsEnumerable().GetEnumerator());

    private sealed class AsyncEnumerator : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        public AsyncEnumerator(IEnumerator<T> enumerator) => _enumerator = enumerator;
        public T Current => _enumerator.Current;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_enumerator.MoveNext());
        public ValueTask DisposeAsync()
        {
            _enumerator.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

internal sealed class AsyncQueryProvider : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    public AsyncQueryProvider(IQueryProvider inner) => _inner = inner;
    public IQueryable CreateQuery(Expression expression) =>
        throw new NotSupportedException();
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncQuery<TElement>(expression);
    public object? Execute(Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var result = typeof(IQueryProvider).GetMethod(nameof(Execute), 1, [typeof(Expression)])!
            .MakeGenericMethod(resultType).Invoke(_inner, [expression]);
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType).Invoke(null, [result])!;
    }
}
