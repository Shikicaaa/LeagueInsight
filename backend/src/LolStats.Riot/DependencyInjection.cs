using LolStats.Domain.Riot;
using LolStats.Riot.Clients;
using LolStats.Riot.Http;
using LolStats.Riot.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LolStats.Riot;

public static class DependencyInjection
{
    public static IServiceCollection AddLolStatsRiot(this IServiceCollection services)
    {
        services.AddOptions<RiotOptions>()
            .BindConfiguration(RiotOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Riot:ApiKey is missing (use user-secrets).")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Platform), "Riot:Platform is missing (e.g. 'eun1').")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Region), "Riot:Region is missing (e.g. 'europe').")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
        services.AddTransient<RateLimitHandler>();

        AddClient<AccountClient>(services, o => RiotHosts.ForRegion(o.Region));
        AddClient<SummonerClient>(services, o => RiotHosts.ForPlatform(o.Platform));
        AddClient<LeagueClient>(services, o => RiotHosts.ForPlatform(o.Platform));
        AddClient<MatchClient>(services, o => RiotHosts.ForRegion(o.Region));

        services.AddTransient<IRiotGateway, RiotGateway>();
        return services;
    }

    private static void AddClient<TClient>(IServiceCollection services, Func<RiotOptions, Uri> host) where TClient : class
    {
        var client = services.AddHttpClient<TClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<RiotOptions>>().Value;
            http.BaseAddress = host(options);
            http.DefaultRequestHeaders.Add("X-Riot-Token", options.ApiKey);
        });

        client.AddStandardResilienceHandler();
        client.AddHttpMessageHandler<RateLimitHandler>();
    }
}