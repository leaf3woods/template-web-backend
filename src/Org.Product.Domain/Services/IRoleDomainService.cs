namespace Org.Product.Domain.Services
{
    public interface IRoleDomainService : IDomainService
    {
        // Null means at least one requested role no longer exists.
        public Task<IEnumerable<string>?> GetPermissionsAsync(params IEnumerable<Guid> roleIds);

        public Task<bool> ExistsInCacheAsync(params IEnumerable<Guid> roleIds);

        public Task CachePermissionsAsync(
            Guid roleId,
            IEnumerable<string> permissions,
            TimeSpan? expiration = null
        );
    }
}
