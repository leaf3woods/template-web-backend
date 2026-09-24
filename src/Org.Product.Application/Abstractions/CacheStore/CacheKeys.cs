using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Abstractions.CacheStore;

public static class CacheKeys
{
    public const string Version = "v1";
    private const string FingerprintVersion = "v1";

    public static string Token(Guid userId) => $"{Version}:sys:token:{userId}";

    public static string Captcha(Guid captchaId) => $"{Version}:sys:captcha:{captchaId}";

    public static string Permissions(Guid roleId) => $"{Version}:sys:permissions:{roleId}";

    public static string Entity<TEntity, TKey>(TKey id) where TEntity : IAggregateRoot => Entity(CacheTypeName<TEntity>.Value, id);
    public static string Entity<TKey>(string entityName, TKey id) => $"{Version}:sys:entity:{entityName.ToLowerInvariant()}:{id}";

    public static string EntitiesGeneration<TEntity>() where TEntity : IAggregateRoot => EntitiesGeneration(CacheTypeName<TEntity>.Value);

    public static string EntitiesGeneration(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        return $"{Version}:sys:entities-generation:{entityName.ToLowerInvariant()}";
    }

    public static string Entities<TEntity>(long generation, string predicateFingerprint) where TEntity : IAggregateRoot =>
        Entities(generation, predicateFingerprint, CacheTypeName<TEntity>.Value);

    public static string Entities(long generation, string predicateFingerprint, params string[] entityNames)
    {
        if(entityNames?.Length is null or 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(predicateFingerprint);
        }
        return $"{Version}:sys:entities:{FingerprintVersion}:{string.Join('|', entityNames!)}:g{generation}:{predicateFingerprint}";
    }

    private static class CacheTypeName<TEntity> where TEntity : IAggregateRoot
    {
        public static string Value { get; } = typeof(TEntity).Name;
    }
}
