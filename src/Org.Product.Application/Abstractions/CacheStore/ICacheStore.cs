namespace Org.Product.Application.Abstractions.CacheStore;

public interface ICacheStore { }

public interface ICacheStore<TId> : ICacheStore
    where TId : notnull, IEquatable<TId>
{
    string Key(TId id);

    Task SaveAsync<TContent>(TId id, TContent content, TimeSpan? expiration = null);

    Task<bool> MatchesAsync(TId id, string expectedValue);

    Task<bool> RemoveAsync(TId id, string? expectedValue = null);
}

public interface ICacheStore<TScope, TId> : ICacheStore<TId>
    where TId : notnull, IEquatable<TId> { }
