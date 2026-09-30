using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Abstractions.CacheStore;

public static class CacheKeys
{
    public const string Version = "v1";
    private const string FingerprintVersion = "v1";

    public static string Token(Guid userId) => $"{Version}:sys:token:{userId}";

    public static string Captcha(Guid captchaId) => $"{Version}:sys:captcha:{captchaId}";

    public static string Permissions(Guid roleId) => $"{Version}:sys:permissions:{roleId}";

    public static string Entity<TEntity, TKey>(TKey id)
        where TEntity : IAggregateRoot => Entity(CacheTypeName<TEntity>.Value, id);

    public static string Entity<TEntity, TKey, TCacheItem>(TKey id, long generation)
        where TEntity : IAggregateRoot =>
        $"{Version}:sys:entity:{CacheTypeName<TEntity>.Value.ToLowerInvariant()}:{id}:{typeof(TCacheItem).FullName}:{generation}";

    public static string Entity<TKey>(string entityName, TKey id) =>
        $"{Version}:sys:entity:{entityName.ToLowerInvariant()}:{id}";

    public static string EntitiesGeneration<TEntity>()
        where TEntity : IAggregateRoot => EntitiesGeneration(CacheTypeName<TEntity>.Value);

    public static string EntitiesGeneration(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        return $"{Version}:sys:entities-generation:{entityName.ToLowerInvariant()}";
    }

    public static string ProjectedEntity<TEntity, TKey, TCacheItem>(
        TKey id,
        params (string name, long generation)[] entityGen
    )
        where TEntity : IAggregateRoot =>
        $"{Version}:sys:projected-entity:{CacheTypeName<TEntity>.Value}:{id}:{typeof(TCacheItem).FullName}:{string.Join('|', entityGen.Select(e => $"{e.name}:{e.generation}"))}";

    public static string Entities<TEntity, TCacheItem>(string predicateFingerprint, long generation)
        where TEntity : IAggregateRoot =>
        Entities<TCacheItem>(predicateFingerprint, (CacheTypeName<TEntity>.Value, generation));

    public static string Entities<TEntity, TRelation, TCacheItem>(
        string predicateFingerprint,
        params long[] generations
    )
        where TEntity : IAggregateRoot
        where TRelation : IAggregateRoot =>
        Entities<TCacheItem>(
            predicateFingerprint,
            [
                (CacheTypeName<TEntity>.Value, generations[0]),
                (CacheTypeName<TRelation>.Value, generations[1]),
            ]
        );

    public static string Entities<TEntity, TRelation1, TRelation2, TCacheItem>(
        string predicateFingerprint,
        params long[] generations
    )
        where TEntity : IAggregateRoot
        where TRelation1 : IAggregateRoot
        where TRelation2 : IAggregateRoot =>
        Entities<TCacheItem>(
            predicateFingerprint,
            [
                (CacheTypeName<TEntity>.Value, generations[0]),
                (CacheTypeName<TRelation1>.Value, generations[1]),
                (CacheTypeName<TRelation2>.Value, generations[2]),
            ]
        );

    public static string Entities<TEntity, TRelation1, TRelation2, TRelation3, TCacheItem>(
        string predicateFingerprint,
        params long[] generations
    )
        where TEntity : IAggregateRoot
        where TRelation1 : IAggregateRoot
        where TRelation2 : IAggregateRoot
        where TRelation3 : IAggregateRoot =>
        Entities<TCacheItem>(
            predicateFingerprint,
            [
                (CacheTypeName<TEntity>.Value, generations[0]),
                (CacheTypeName<TRelation1>.Value, generations[1]),
                (CacheTypeName<TRelation2>.Value, generations[2]),
                (CacheTypeName<TRelation3>.Value, generations[3]),
            ]
        );

    public static string Entities<TCacheItem>(
        string predicateFingerprint,
        params (string name, long generation)[] entityGen
    )
    {
        if (entityGen?.Length is null or 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(predicateFingerprint);
        }
        return $"{Version}:sys:entities:{FingerprintVersion}:{string.Join('|', entityGen!.Select(e => $"{e.name}:{e.generation}"))}:{typeof(TCacheItem).FullName}:{predicateFingerprint}";
    }

    private static class CacheTypeName<TEntity>
        where TEntity : IAggregateRoot
    {
        public static string Value { get; } = typeof(TEntity).Name;
    }
}
