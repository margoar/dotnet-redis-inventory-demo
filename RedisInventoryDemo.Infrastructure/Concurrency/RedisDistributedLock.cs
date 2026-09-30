using RedisInventoryDemo.Application.Abstractions.Concurrency;
using StackExchange.Redis;

namespace RedisInventoryDemo.Infrastructure.Concurrency;

public sealed class RedisDistributedLock : IDistributedLock
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisDistributedLock(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<string?> TryAcquireAsync(
        string key,
        TimeSpan expiration)
    {
        var database = _connectionMultiplexer.GetDatabase();

        var token = Guid.NewGuid().ToString();

        var acquired = await database.StringSetAsync(
            key,
            token,
            expiration,
            When.NotExists);

        return acquired ? token : null;
    }

    public async Task ReleaseAsync(string key, string token)
    {
        var database = _connectionMultiplexer.GetDatabase();

        const string releaseScript = """
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('DEL', KEYS[1])
            end

            return 0
            """;

        await database.ScriptEvaluateAsync(releaseScript, [key], [token]);
    }
}