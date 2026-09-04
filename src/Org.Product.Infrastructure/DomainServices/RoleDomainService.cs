using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Services;
using Org.Product.Domain.Shared;
using Org.Product.Infrastructure.Repositories;
using StackExchange.Redis;

namespace Org.Product.Infrastructure.DomainServices
{
    public class RoleDomainService : IRoleDomainService
    {
        public RoleDomainService(ApiDbContext apiDbContext, IConnectionMultiplexer connectionMultiplexer)
        {
            _apiDbContext = apiDbContext;
            _connectionMultiplexer = connectionMultiplexer;
        }

        private readonly ApiDbContext _apiDbContext;
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public async Task<IEnumerable<string>> GetPermissionsAsync(Guid roleId)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Permissions, roleId);
            var raw = await database.StringGetAsync(key);
            if (!raw.HasValue)
            {
                return [];
            }

            return JsonSerializer.Deserialize<IEnumerable<string>>(raw.ToString()) ?? [];
        }

        public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetPermissionsAsync(
            IEnumerable<Guid> roleIds,
            CancellationToken cancellationToken = default
        )
        {
            var distinctRoleIds = roleIds.Distinct().ToArray();
            if (distinctRoleIds.Length == 0)
            {
                return new Dictionary<Guid, IReadOnlyCollection<string>>();
            }

            var roles = await _apiDbContext
                .Roles.AsNoTracking()
                .Include(role => role.Permissions)
                .Where(role => distinctRoleIds.Contains(role.Id))
                .ToArrayAsync(cancellationToken);

            return roles.ToDictionary(
                role => role.Id,
                role => (IReadOnlyCollection<string>)(role.Permissions ?? [])
                    .Select(permission => permission.Name)
                    .ToArray()
            );
        }

        public async Task CachePermissionsAsync(
            Guid roleId,
            IEnumerable<string> permissions,
            TimeSpan? expiration = null
        )
        {
            var database = _connectionMultiplexer.GetDatabase();
            var key = string.Format(CacheKeyFormatter.Permissions, roleId);
            var json = JsonSerializer.Serialize(permissions);
            var task = expiration is null
                ? database.StringSetAsync(key, json)
                : database.StringSetAsync(key, json, expiration.Value);
            await task;
        }
    }
}
