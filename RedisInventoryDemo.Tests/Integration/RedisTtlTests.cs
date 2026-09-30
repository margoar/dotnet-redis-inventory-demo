using StackExchange.Redis;

namespace RedisInventoryDemo.Tests.Integration;

public class RedisTtlTests
{
    [Fact]
    public async Task RedisKey_ShouldExpire_WhenTtlIsReached()
    {
        // Arrange
        var connection = await ConnectionMultiplexer.ConnectAsync("localhost:6380");

        var database = connection.GetDatabase();

        const string key = "inventory:test:ttl";

        await database.KeyDeleteAsync(key);

        await database.StringSetAsync(key,  10, TimeSpan.FromSeconds(1));

        // Act
        var existsBeforeExpiration = await database.KeyExistsAsync(key);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var existsAfterExpiration = await database.KeyExistsAsync(key);

        // Assert
        Assert.True(existsBeforeExpiration);
        Assert.False(existsAfterExpiration);

        await connection.CloseAsync();
    }
}