using Org.Product.Domain.Entities.Base;

namespace Org.Product.Application.Abstractions.CacheStore;

public static class CacheKeys
{
    public const string Version = "v1";

    public static string Token(Guid userId) => $"{Version}:sys:token:{userId}";

    public static string Captcha(Guid captchaId) => $"{Version}:sys:captcha:{captchaId}";

    public static string Permissions(Guid roleId) => $"{Version}:sys:permissions:{roleId}";

    public static string Entity<TEntity, TKey>(TKey id) where TEntity : IAggregateRoot => $"{Version}:sys:entity:{CacheTypeName<TEntity>.Value}:{id}";

    public static string Entities<TEntity>(string predicateFingerprint) where TEntity : IAggregateRoot
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(predicateFingerprint);
        return $"{Version}:sys:entities:{CacheTypeName<TEntity>.Value}:{predicateFingerprint}";
    }

    private static class CacheTypeName<TEntity> where TEntity : IAggregateRoot
    {
        public static string Value { get; } =
            typeof(TEntity).Name.ToLowerInvariant();
    }
}
