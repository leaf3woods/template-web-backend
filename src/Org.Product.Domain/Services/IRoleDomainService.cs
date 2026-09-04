namespace Org.Product.Domain.Services
{
    public interface IRoleDomainService : IDomainService
    {
        public Task<IEnumerable<string>> GetPermissionsAsync(Guid roleId);

        public Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetPermissionsAsync(
            IEnumerable<Guid> roleIds,
            CancellationToken cancellationToken = default
        );

        public Task CachePermissionsAsync(
            Guid roleId,
            IEnumerable<string> permissions,
            TimeSpan? expiration = null
        );
    }
}
