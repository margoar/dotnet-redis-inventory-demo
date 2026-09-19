using Microsoft.Extensions.DependencyInjection;
using RedisInventoryDemo.Application.Abstractions.Persistence;
using RedisInventoryDemo.Application.Services;
using RedisInventoryDemo.Infrastructure.Persistence;

namespace RedisInventoryDemo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<IInventoryRepository, InMemoryInventoryRepository>();
        services.AddScoped<IInventoryService, InventoryService>();

        return services;
    }
}