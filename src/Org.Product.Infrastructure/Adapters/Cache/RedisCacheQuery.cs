using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
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

namespace Org.Product.Infrastructure.Adapters.Cache;

public sealed class RedisCacheQuery<TEntity, TKey, TCacheItem>
    : ICacheQuery<TEntity, TKey, TCacheItem>
    where TEntity : AggregateRoot<TKey>
    where TKey : notnull, IEquatable<TKey>, new()
    where TCacheItem : CacheItem<TEntity, TKey>
{
    public RedisCacheQuery(
        IRepository<TEntity> repository,
        IConnectionMultiplexer connectionMultiplexer,
        IMapper mapper,
        IOptions<CacheOptions> options
    )
    {
        _repository = repository;
        _connectionMultiplexer = connectionMultiplexer;
        _mapper = mapper;
        _options = options.Value;
        _database = new Lazy<IDatabase>(() => _connectionMultiplexer.GetDatabase());
    }

    private readonly IRepository<TEntity> _repository;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IMapper _mapper;
    private readonly CacheOptions _options;
    private readonly Lazy<IDatabase> _database;

    public async Task<TCacheItem?> FindAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var database = _database.Value;
        var generationValue = await database.StringGetAsync(
            CacheKeys.EntitiesGeneration<TEntity>()
        );
        var generation = generationValue.HasValue
            ? long.Parse(generationValue.ToString(), CultureInfo.InvariantCulture)
            : 0;
        var key = CacheKeys.Entity<TEntity, TKey, TCacheItem>(id, generation);
        var raw = await database.StringGetAsync(key);
        if (raw.HasValue)
        {
            var plain = raw.ToString();
            return JsonSerializer.Deserialize<TCacheItem>(plain);
        }

        var entity =
            (await _repository.FindAsync(id))
            ?? throw new NotFoundException("entity not found in cache and repository");
        var cacheItem = _mapper.Map<TCacheItem>(entity);
        var serialized = JsonSerializer.Serialize(cacheItem);
        await database.StringSetAsync(key, serialized, _options.Expiration);
        return cacheItem;
    }

    public async Task<IReadOnlyDictionary<TKey, TCacheItem>> FindManyAsync(
        IReadOnlyCollection<TKey> ids,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return new Dictionary<TKey, TCacheItem>();
        }

        var distinctIds = ids.Distinct().ToArray();
        var database = _database.Value;
        var generationValue = await database.StringGetAsync(
            CacheKeys.EntitiesGeneration<TEntity>()
        );
        var generation = generationValue.HasValue
            ? long.Parse(generationValue.ToString(), CultureInfo.InvariantCulture)
            : 0;
        var keys = distinctIds
            .Select(id => (RedisKey)CacheKeys.Entity<TEntity, TKey, TCacheItem>(id, generation))
            .ToArray();
        var cachedValues = await database.StringGetAsync(keys);
        var items = new Dictionary<TKey, TCacheItem>(distinctIds.Length);
        var missingIds = new List<TKey>();
        for (var index = 0; index < distinctIds.Length; index++)
        {
            var item = cachedValues[index].HasValue
                ? JsonSerializer.Deserialize<TCacheItem>(cachedValues[index].ToString())
                : null;
            if (item is null)
            {
                missingIds.Add(distinctIds[index]);
            }
            else
            {
                items.Add(distinctIds[index], item);
            }
        }

        if (missingIds.Count == 0)
        {
            return items;
        }

        var entities = await _repository
            .Query(tracking: false)
            .Where(entity => missingIds.Contains(entity.Id))
            .ToListAsync(cancellationToken);
        var fetchedItems = entities.ToDictionary(
            entity => entity.Id,
            entity => _mapper.Map<TCacheItem>(entity)
        );
        foreach (var (id, item) in fetchedItems)
        {
            items.Add(id, item);
        }

        await Task.WhenAll(
            fetchedItems.Select(entry =>
                database.StringSetAsync(
                    CacheKeys.Entity<TEntity, TKey, TCacheItem>(entry.Key, generation),
                    JsonSerializer.Serialize(entry.Value),
                    _options.Expiration
                )
            )
        );
        return items;
    }

    public Task<TCacheItem?> FindWithDetailAsync(
        TKey id,
        CancellationToken cancellationToken = default
    ) =>
        FindWithDetailCoreAsync(
            id,
            [(CacheTypeName<TEntity>.Value, CacheKeys.EntitiesGeneration<TEntity>())],
            cancellationToken
        );

    public Task<TCacheItem?> FindWithDetailAsync<TRelation>(
        TKey id,
        CancellationToken cancellationToken = default
    )
        where TRelation : IAggregateRoot =>
        FindWithDetailCoreAsync(
            id,
            [
                (CacheTypeName<TEntity>.Value, CacheKeys.EntitiesGeneration<TEntity>()),
                (CacheTypeName<TRelation>.Value, CacheKeys.EntitiesGeneration<TRelation>()),
            ],
            cancellationToken
        );

    public Task<TCacheItem?> FindWithDetailAsync<TRelation1, TRelation2>(
        TKey id,
        CancellationToken cancellationToken = default
    )
        where TRelation1 : IAggregateRoot
        where TRelation2 : IAggregateRoot =>
        FindWithDetailCoreAsync(
            id,
            [
                (CacheTypeName<TEntity>.Value, CacheKeys.EntitiesGeneration<TEntity>()),
                (CacheTypeName<TRelation1>.Value, CacheKeys.EntitiesGeneration<TRelation1>()),
                (CacheTypeName<TRelation2>.Value, CacheKeys.EntitiesGeneration<TRelation2>()),
            ],
            cancellationToken
        );

    public Task<IReadOnlyDictionary<TKey, TCacheItem>> FindManyWithDetailAsync(
        IReadOnlyCollection<TKey> ids,
        CancellationToken cancellationToken = default
    ) =>
        FindManyWithDetailCoreAsync(
            ids,
            [(CacheTypeName<TEntity>.Value, CacheKeys.EntitiesGeneration<TEntity>())],
            cancellationToken
        );

    public Task<IReadOnlyDictionary<TKey, TCacheItem>> FindManyWithDetailAsync<TRelation>(
        IReadOnlyCollection<TKey> ids,
        CancellationToken cancellationToken = default
    )
        where TRelation : IAggregateRoot =>
        FindManyWithDetailCoreAsync(
            ids,
            [
                (CacheTypeName<TEntity>.Value, CacheKeys.EntitiesGeneration<TEntity>()),
                (CacheTypeName<TRelation>.Value, CacheKeys.EntitiesGeneration<TRelation>()),
            ],
            cancellationToken
        );

    public Task<IReadOnlyDictionary<TKey, TCacheItem>> FindManyWithDetailAsync<
        TRelation1,
        TRelation2
    >(IReadOnlyCollection<TKey> ids, CancellationToken cancellationToken = default)
        where TRelation1 : IAggregateRoot
        where TRelation2 : IAggregateRoot =>
        FindManyWithDetailCoreAsync(
            ids,
            [
                (CacheTypeName<TEntity>.Value, CacheKeys.EntitiesGeneration<TEntity>()),
                (CacheTypeName<TRelation1>.Value, CacheKeys.EntitiesGeneration<TRelation1>()),
                (CacheTypeName<TRelation2>.Value, CacheKeys.EntitiesGeneration<TRelation2>()),
            ],
            cancellationToken
        );

    private async Task<TCacheItem?> FindWithDetailCoreAsync(
        TKey id,
        (string name, string key)[] dependencies,
        CancellationToken cancellationToken
    )
    {
        var items = await FindManyWithDetailCoreAsync([id], dependencies, cancellationToken);
        return items.TryGetValue(id, out var item) ? item : null;
    }

    private async Task<IReadOnlyDictionary<TKey, TCacheItem>> FindManyWithDetailCoreAsync(
        IReadOnlyCollection<TKey> ids,
        (string name, string key)[] dependencies,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return new Dictionary<TKey, TCacheItem>();
        }

        var distinctIds = ids.Distinct().ToArray();
        var database = _database.Value;
        var generationValues = await database.StringGetAsync(
            dependencies.Select(dependency => (RedisKey)dependency.key).ToArray()
        );
        var generations = dependencies
            .Select(
                (dependency, index) =>
                    (
                        dependency.name,
                        generationValues[index].HasValue
                            ? long.Parse(
                                generationValues[index].ToString(),
                                CultureInfo.InvariantCulture
                            )
                            : 0
                    )
            )
            .ToArray();

        var keys = distinctIds
            .Select(id =>
                (RedisKey)CacheKeys.ProjectedEntity<TEntity, TKey, TCacheItem>(id, generations)
            )
            .ToArray();
        var cachedValues = await database.StringGetAsync(keys);
        var items = new Dictionary<TKey, TCacheItem>(distinctIds.Length);
        var missingIds = new List<TKey>();
        for (var index = 0; index < distinctIds.Length; index++)
        {
            var item = cachedValues[index].HasValue
                ? JsonSerializer.Deserialize<TCacheItem>(cachedValues[index].ToString())
                : null;
            if (item is null)
            {
                missingIds.Add(distinctIds[index]);
            }
            else
            {
                items.Add(distinctIds[index], item);
            }
        }

        if (missingIds.Count == 0)
        {
            return items;
        }

        var fetchedItems = await _repository
            .Query(tracking: false)
            .Where(entity => missingIds.Contains(entity.Id))
            .ProjectTo<TCacheItem>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        foreach (var item in fetchedItems)
        {
            items[item.Id] = item;
        }

        await Task.WhenAll(
            fetchedItems.Select(item =>
                database.StringSetAsync(
                    CacheKeys.ProjectedEntity<TEntity, TKey, TCacheItem>(item.Id, generations),
                    JsonSerializer.Serialize(item),
                    _options.Expiration
                )
            )
        );
        return items;
    }

    public async Task<IReadOnlyList<TCacheItem>> GetFilteredListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        if (!FingerprintVisitor.TryCreate(predicate, out var fingerprint))
        {
            return await QueryAsync(predicate, cancellationToken);
        }

        var database = _database.Value;
        var generationValue = await database.StringGetAsync(
            CacheKeys.EntitiesGeneration<TEntity>()
        );
        var generation = generationValue.HasValue
            ? long.Parse(generationValue.ToString(), CultureInfo.InvariantCulture)
            : 0;
        var key = CacheKeys.Entities<TEntity, TCacheItem>(fingerprint, generation);
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

    public async Task<IReadOnlyList<TCacheItem>> GetFilteredListAsync<TRelation>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
        where TRelation : IAggregateRoot
    {
        if (!FingerprintVisitor.TryCreate(predicate, out var fingerprint))
        {
            return await QueryAsync(predicate, cancellationToken);
        }

        var database = _database.Value;
        var generationKeys = new string[]
        {
            CacheKeys.EntitiesGeneration<TEntity>(),
            CacheKeys.EntitiesGeneration<TRelation>(),
        };
        var generationTasks = generationKeys.Select(async g =>
        {
            var gValue = await database.StringGetAsync(g);
            var generation = gValue.HasValue
                ? long.Parse(gValue.ToString(), CultureInfo.InvariantCulture)
                : 0;
            return generation;
        });
        var generations = await Task.WhenAll(generationTasks);
        var key = CacheKeys.Entities<TEntity, TRelation, TCacheItem>(fingerprint, generations);
        return await GetFilteredListCoreAsync(predicate, key, cancellationToken);
    }

    public async Task<IReadOnlyList<TCacheItem>> GetFilteredListAsync<TRelation1, TRelation2>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
        where TRelation1 : IAggregateRoot
        where TRelation2 : IAggregateRoot
    {
        if (!FingerprintVisitor.TryCreate(predicate, out var fingerprint))
        {
            return await QueryAsync(predicate, cancellationToken);
        }

        var database = _database.Value;
        var generationKeys = new string[]
        {
            CacheKeys.EntitiesGeneration<TEntity>(),
            CacheKeys.EntitiesGeneration<TRelation1>(),
            CacheKeys.EntitiesGeneration<TRelation2>(),
        };
        var generationTasks = generationKeys.Select(async g =>
        {
            var gValue = await database.StringGetAsync(g);
            var generation = gValue.HasValue
                ? long.Parse(gValue.ToString(), CultureInfo.InvariantCulture)
                : 0;
            return generation;
        });
        var generations = await Task.WhenAll(generationTasks);
        var key = CacheKeys.Entities<TEntity, TRelation1, TRelation2, TCacheItem>(
            fingerprint,
            generations
        );
        return await GetFilteredListCoreAsync(predicate, key, cancellationToken);
    }

    private async Task<IReadOnlyList<TCacheItem>> GetFilteredListCoreAsync(
        Expression<Func<TEntity, bool>> predicate,
        string key,
        CancellationToken cancellationToken = default
    )
    {
        if (!FingerprintVisitor.TryCreate(predicate, out var fingerprint))
        {
            return await QueryAsync(predicate, cancellationToken);
        }

        var database = _database.Value;
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
        CancellationToken cancellationToken
    )
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
