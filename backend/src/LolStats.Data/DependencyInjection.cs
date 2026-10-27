using LolStats.Data.Outbox;
using LolStats.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LolStats.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddLolStatsData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<LolStatsDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
        );

        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}