using System.Net;
using LolStats.Domain.Riot;
using LolStats.Domain.Jobs;
using LolStats.Riot.RateLimiting;

using Microsoft.Extensions.Options;

namespace LolStats.Riot.Http;

internal sealed class RateLimitHandler(IRateLimiter limiter, IOptions<RiotOptions> options) : DelegatingHandler
{
    private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromSeconds(5);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var host = request.RequestUri!.Host;
        var method = request.Options.TryGetValue(RiotRequestOptions.Method, out var m) ? m : "unknown";
        var appKey = $"rl:{host}:app";
        var methodKey = $"rl:{host}:method:{method}";
        var priority = JobContext.Priority;

        while (true)
        {
            var wait = limiter.TryAcquire(appKey, methodKey, priority);
            if (wait is null) break;

            if (priority >= JobPriority.P2 || wait > options.Value.MaxInProcessWait)
                throw new RiotRateLimitException(wait.Value);
            
            await Task.Delay(wait.Value, ct);
        }

        var response = await base.SendAsync(request, ct);

        limiter.Sync(appKey, RateLimitWindows.Parse(response, "X-App-Rate-Limit"));
        limiter.Sync(methodKey, RateLimitWindows.Parse(response, "X-Method-Rate-Limit"));

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta ?? DefaultRetryAfter;
            var type = response.Headers.TryGetValues("X-Rate-Limit-Type", out var values) ? values.FirstOrDefault() : null;

            if (type == "application")
                limiter.Pause(appKey, retryAfter);
            else if (type == "method")
                limiter.Pause(methodKey, retryAfter);
            
            response.Dispose();
            throw new RiotRateLimitException(retryAfter);
        }

        return response;
    }
}