namespace Org.Product.Application.Abstractions.CacheStore;

public sealed class CacheQueryResultTooLargeException : InvalidOperationException
{
    public CacheQueryResultTooLargeException(Type entityType, int maximumItems)
        : base(
            $"The cached query for '{entityType.Name}' exceeded the maximum of "
                + $"{maximumItems} items. Use a paginated query instead."
        )
    {
        EntityType = entityType;
        MaximumItems = maximumItems;
    }

    public Type EntityType { get; }

    public int MaximumItems { get; }
}
