using RedisInventoryDemo.Infrastructure.Concurrency;
using StackExchange.Redis;

namespace RedisInventoryDemo.Tests.Integration;

public class RedisDistributedLockTests
{
    [Fact]
    public async Task TryAcquireAsync_ShouldAllowOnlyOneOwner()
    {
        // Arrange
        var connection = await ConnectionMultiplexer.ConnectAsync("localhost:6380");

        var database = connection.GetDatabase();

        const string key = "lock:test:single-owner";

        await database.KeyDeleteAsync(key);

        var distributedLock = new RedisDistributedLock(connection);

        // Act
        var firstToken = await distributedLock.TryAcquireAsync(key,TimeSpan.FromSeconds(10));

        var secondToken = await distributedLock.TryAcquireAsync(key,TimeSpan.FromSeconds(10));

        // Assert
        Assert.NotNull(firstToken);
        Assert.Null(secondToken);

        await distributedLock.ReleaseAsync(key, firstToken!);

        await connection.CloseAsync();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldAllowAnotherOwnerToAcquire()
    {
        // Arrange
        var connection = await ConnectionMultiplexer.ConnectAsync("localhost:6380");

        var database = connection.GetDatabase();

        const string key = "lock:test:release";

        await database.KeyDeleteAsync(key);

        var distributedLock = new RedisDistributedLock(connection);

        // Act
        var firstToken = await distributedLock.TryAcquireAsync(key,TimeSpan.FromSeconds(10));

        Assert.NotNull(firstToken);

        await distributedLock.ReleaseAsync(key, firstToken!);

        var secondToken = await distributedLock.TryAcquireAsync(key, TimeSpan.FromSeconds(10));

        // Assert
        Assert.NotNull(secondToken);
        Assert.NotEqual(firstToken, secondToken);

        await distributedLock.ReleaseAsync(key, secondToken!);

        await database.KeyDeleteAsync(key);
        await connection.CloseAsync();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldNotReleaseLockOwnedByAnotherToken()
    {
        // Arrange
        var connection = await ConnectionMultiplexer.ConnectAsync("localhost:6380");

        var database = connection.GetDatabase();

        const string key = "lock:test:ownership";

        await database.KeyDeleteAsync(key);

        var distributedLock = new RedisDistributedLock(connection);

        var ownerToken = await distributedLock.TryAcquireAsync(key, TimeSpan.FromSeconds(10));

        Assert.NotNull(ownerToken);

        // Act
        await distributedLock.ReleaseAsync(key, "fake-token");

        var secondToken = await distributedLock.TryAcquireAsync(key, TimeSpan.FromSeconds(10));

        // Assert
        Assert.Null(secondToken);

        await distributedLock.ReleaseAsync(key, ownerToken!);

        await database.KeyDeleteAsync(key);
        await connection.CloseAsync();
    }
}