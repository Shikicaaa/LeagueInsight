using Hangfire;
using LolStats.Domain.Abstractions;
using LolStats.Domain.Jobs;
using LolStats.Domain.Riot;
using Microsoft.Extensions.Logging;

namespace LolStats.Infrastructure.Jobs;

// The only type Hangfire ever sees. Api enqueues JobRunner<RefreshPlayer> without knowing the Worker,
// and the Worker resolves IJobHandler<RefreshPlayer> from DI when the job runs.
// HTTP-level retries (5xx, timeouts) are already done by the resilience handler, so few job retries are needed.
[AutomaticRetry(Attempts = 3)]
public sealed class JobRunner<TJob>(
    IJobHandler<TJob> handler,
    IJobQueue queue,
    ILogger<JobRunner<TJob>> logger) where TJob : JobRequest
{
    // "{0}" shows the record's ToString in the dashboard, e.g. RefreshPlayer { Puuid = ..., Priority = P0 }.
    [JobDisplayName("{0}")]
    public async Task RunAsync(TJob job, CancellationToken ct)
    {
        // Every Riot call inside the handler sees this priority (P2/P3 keep a reserve and never wait).
        using var _ = JobContext.Begin(job.Priority);

        try
        {
            await handler.HandleAsync(job, ct);
        }
        catch (RiotRateLimitException ex)
        {
            // A 429 is not a failure: schedule the same request again and finish this job successfully,
            // so no retry attempt is spent.
            var jobId = queue.Schedule(job, ex.RetryAfter);
            logger.LogInformation("{Job} rate limited, rescheduled as {JobId} in {RetryAfter}", job, jobId, ex.RetryAfter);
        }
    }
}
