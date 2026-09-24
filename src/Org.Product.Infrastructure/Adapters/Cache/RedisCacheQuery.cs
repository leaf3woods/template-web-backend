using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Application.Dtos.Base;
using Org.Product.Domain.Entities.Base;
using Org.Product.Domain.Repositories;
using Org.Product.Domain.Shared.Exceptions;
using Org.Product.Domain.Shared.Expressions;
using Org.Product.Infrastructure.Utilities.Options;
using StackExchange.Redis;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace Org.Product.Infrastructure.Adapters.Cache;

public sealed class RedisCacheQuery<TEntity, TKey, TCacheItem> : ICacheQuery<TEntity, TKey, TCacheItem>
    where TEntity : class, IAggregateRoot
    where TKey : notnull
    where TCacheItem : CacheItem<TEntity, TKey>
{
    public RedisCacheQuery(IRepository<TEntity> repository,
        IConnectionMultiplexer connectionMultiplexer,
        IMapper mapper,
        IOptions<CacheOptions> options)
    {
        _repository = repository;
        _connectionMultiplexer = connectionMultiplexer;
        _mapper = mapper;
        _options = options.Value;
    }

    private readonly IRepository<TEntity> _repository;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IMapper _mapper;
    private readonly CacheOptions _options;

    public async Task<TCacheItem?> FindAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var key = CacheKeys.Entity<TEntity, TKey>(id);
        var raw = await database.StringGetAsync(key);
        if (raw.HasValue)
        {
            var plain = raw.ToString();
            return JsonSerializer.Deserialize<TCacheItem>(plain);
        }

        var entity = (await _repository.FindAsync(id)) ?? throw new NotFoundException("entity not found in cache and repository");
        var cacheItem = _mapper.Map<TCacheItem>(entity);
        var serialized = JsonSerializer.Serialize(cacheItem);
        await database.StringSetAsync(key, serialized, _options.Expiration);
        return cacheItem;
    }

    public async Task<IReadOnlyList<TCacheItem>> GetFilteredListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        if (!FingerprintVisitor.TryCreate(predicate, out var fingerprint))
        {
            return await QueryAsync(predicate, cancellationToken);
        }

        var database = _connectionMultiplexer.GetDatabase();
        var generationValue = await database.StringGetAsync(CacheKeys.EntitiesGeneration<TEntity>());
        var generation = generationValue.HasValue
            ? long.Parse(generationValue.ToString(), CultureInfo.InvariantCulture)
            : 0;
        var key = CacheKeys.Entities<TEntity>(generation, fingerprint);
        var raw = await database.StringGetAsync(key);
        if (raw.HasValue)
        {
            var cachedItems = JsonSerializer.Deserialize<TCacheItem[]>(raw.ToString()) ?? [];
            return cachedItems;
        }

        var cacheItems = await QueryAsync(predicate, cancellationToken);
        var serialized = JsonSerializer.Serialize(cacheItems);
        await database.StringSetAsync(key, serialized, _options.Expiration);
        return cacheItems;
    }

    private async Task<TCacheItem[]> QueryAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var queryLimit = checked(_options.MaxCollectionItems + 1);
        var cacheItems = await _repository
            .Query(tracking: false)
            .Where(predicate)
            .Take(queryLimit)
            .ProjectTo<TCacheItem>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        if (cacheItems.Count > _options.MaxCollectionItems)
        {
            throw new CacheQueryResultTooLargeException(
                typeof(TEntity),
                _options.MaxCollectionItems
            );
        }
        return [.. cacheItems];
    }
}
