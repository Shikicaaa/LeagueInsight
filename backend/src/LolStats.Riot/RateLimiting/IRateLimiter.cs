using LolStats.Domain.Jobs;

namespace LolStats.Riot.RateLimiting;

public interface IRateLimiter
{
    TimeSpan? TryAcquire(string appKey, string methodKey, JobPriority priority);

    void Sync(string key, IReadOnlyList<RateLimitWindow> windows);

    void Pause(string key, TimeSpan retryAfter);
}