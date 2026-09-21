using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedisInventoryDemo.Application.Abstractions.Persistence;
using RedisInventoryDemo.Application.Services;
using RedisInventoryDemo.Infrastructure.Configuration;
using RedisInventoryDemo.Infrastructure.Persistence;
using StackExchange.Redis;



namespace RedisInventoryDemo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,  IConfiguration configuration)
    {

        services.Configure<RedisOptions>(
          configuration.GetSection(RedisOptions.SectionName));

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp
                .GetRequiredService<IOptions<RedisOptions>>()
                .Value;

            return ConnectionMultiplexer.Connect(
                options.ConnectionString);
        });


        services.AddScoped<IInventoryRepository, InMemoryInventoryRepository>();
        services.AddScoped<IInventoryService, InventoryService>();

        return services;
    }
}