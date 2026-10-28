using LolStats.Domain.Abstractions;
using LolStats.Domain.Jobs;

namespace LolStats.Worker.Jobs;

// Skeleton for step 2. Right now it only proves the pipeline: Api enqueues -> Hangfire -> this handler.
public sealed class RefreshPlayerJob(ILogger<RefreshPlayerJob> logger, IDistributedLock locks) : IJobHandler<RefreshPlayer>
{
    public async Task HandleAsync(RefreshPlayer job, CancellationToken ct)
    {
        // logger.LogInformation("Refreshing {Puuid} at priority {Priority}", job.Puuid, JobContext.Priority);

        // // TODO (step 2, flow 2 in CLAUDE.md):
        // // 1. lock:job:{puuid}, held until the end
        // // 2. summoner + league -> update Player, LP snapshot, RankChanged in the outbox if rank changed
        // // 3. match ids from last_match_ts (startTime), count = 10, per ranked queue
        // // 4. diff with the database (WHERE match_id = ANY(@ids)), fetch only new matches
        // // 5. one transaction: matches, participants, outbox MatchIngested, player.RecordIngestedMatch
        // // 6. player.MarkRefreshed, release lock
        
        // return Task.CompletedTask;
        await using var handle = await locks.TryAcquireAsync($"lock:job:{job.Puuid}", TimeSpan.FromMinutes(2), ct);
        if (handle is null)
        {
            logger.LogInformation("{Puuid} is already being refreshed by another worker, skipping", job.Puuid);
            return;
        }

        logger.LogInformation("Refreshing {Puuid} at priority {Priority}", job.Puuid, JobContext.Priority);
    }
}
