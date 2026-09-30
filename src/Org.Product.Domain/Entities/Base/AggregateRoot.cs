namespace Org.Product.Domain.Entities.Base;

public class AggregateRoot<TKey> : IAggregateRoot
    where TKey : notnull, IEquatable<TKey>, new()
{
    public TKey Id { get; set; } = new TKey();
}
