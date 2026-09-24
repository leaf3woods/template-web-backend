namespace Org.Product.Application.Abstractions.CacheStore;

public interface ICacheInvalidator : ICacheStore
{
    Task<int> InvalidateEntityCacheAsync(
        string entityName,
        IReadOnlyCollection<object> ids
    );

    Task InvalidateEntityListCacheAsync(string entityName);

    Task<int> InvalidateStoreCacheAsync(IReadOnlyCollection<string> keys);
}
