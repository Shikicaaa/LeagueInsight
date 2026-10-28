namespace LolStats.Domain.Riot;

// Thrown when a request cannot be made now, job should reschedule it after RetryAfter,
// not retried immediately and the attempt must not count as a failure

public sealed class RiotRateLimitException(TimeSpan retryAfter)
    : Exception($"Riot rate limit exceeded, retry after {retryAfter.TotalSeconds:0.#} seconds.")
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}