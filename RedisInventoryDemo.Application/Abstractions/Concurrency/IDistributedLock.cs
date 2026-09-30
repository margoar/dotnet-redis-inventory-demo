namespace RedisInventoryDemo.Application.Abstractions.Concurrency;

public interface IDistributedLock
{
    Task<string?> TryAcquireAsync( string key,TimeSpan expiration);
    Task ReleaseAsync(string key, string token);
}