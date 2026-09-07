using System.Collections.Concurrent;
using Moq;
using StackExchange.Redis;

namespace Org.Product.Tests.Support;

internal sealed class RedisStub
{
    public ConcurrentDictionary<RedisKey, RedisValue> Values { get; } = new();
    public Mock<IDatabase> Database { get; } = new(MockBehavior.Strict);
    public Mock<IConnectionMultiplexer> Connection { get; } = new();

    public RedisStub()
    {
        Connection.Setup(connection => connection.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(Database.Object);
        Database.Setup(database => database.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) => Values.GetValueOrDefault(key, RedisValue.Null));
        Database.Setup(database => database.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey[] keys, CommandFlags _) => keys.Select(key => Values.GetValueOrDefault(key, RedisValue.Null)).ToArray());
        Database.Setup(database => database.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(), It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue value, Expiration expiry, ValueCondition when, CommandFlags flags) =>
            {
                if (when.Equals((ValueCondition)When.NotExists))
                {
                    return Values.TryAdd(key, value);
                }
                Values[key] = value;
                return true;
            });
        Database.Setup(database => database.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) => Values.TryRemove(key, out var _));
        Database.Setup(database => database.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) => Values.ContainsKey(key));
        Database.Setup(database => database.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((string script, RedisKey[] keys, RedisValue[] values, CommandFlags _) =>
            {
                var removed = ((ICollection<KeyValuePair<RedisKey, RedisValue>>)Values)
                    .Remove(new KeyValuePair<RedisKey, RedisValue>(keys.Single(), values.Single()));
                return RedisResult.Create((RedisValue)(removed ? 1 : 0));
            });
    }
}
