using Hangfire;
using Hangfire.PostgreSql;
using LolStats.Domain.Abstractions;
using LolStats.Infrastructure.Jobs;
using LolStats.Infrastructure.Locking;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LolStats.Infrastructure;

public static class DependencyInjection
{
    // Api and Worker: Hangfire storage (separate database) and IJobQueue for enqueuing.
    public static IServiceCollection AddLolStatsJobs(this IServiceCollection services, string hangfireConnectionString)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                o => o.UseNpgsqlConnection(hangfireConnectionString),
                // LISTEN/NOTIFY: the Worker picks up a new job right away instead of polling every 15 s.
                new PostgreSqlStorageOptions { EnableLongPolling = true }));

        services.AddSingleton<IJobQueue, HangfireJobQueue>();
        return services;
    }

    public static IServiceCollection AddLolStatsValkey(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
        services.AddSingleton<IDistributedLock, ValkeyDistributedLock>();
        return services;
    }

    // Worker only: the server that actually runs jobs.
    public static IServiceCollection AddLolStatsJobServer(this IServiceCollection services)
    {
        services.AddHangfireServer(o => o.Queues = JobQueues.All);
        return services;
    }
}
