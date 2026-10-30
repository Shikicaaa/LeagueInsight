using LolStats.Data;
using LolStats.Domain.Abstractions;
using LolStats.Domain.Events;
using LolStats.Domain.Jobs;
using LolStats.Worker.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace LolStats.Worker.Jobs;

// Flow 4: walks the player's history back one page from HistoryCoveredUntil.
// The cursor is read from the database, not sent by the client, so a repeated request loads the same page.
public sealed class LoadMoreMatchesJob(
    ILogger<LoadMoreMatchesJob> logger, IDistributedLock locks, IJobQueue queue,
    LolStatsDbContext db, MatchIngestor ingestor, IEventPublisher events) : IJobHandler<LoadMoreMatches>
{
    private static readonly TimeSpan RetryWhenLocked = TimeSpan.FromSeconds(15);

    public async Task HandleAsync(LoadMoreMatches job, CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync(PlayerLock.Key(job.Puuid), PlayerLock.Ttl, ct);
        if (handle is null)
        {
            // Unlike a refresh, another running job doesn't do this one's work, so try again shortly.
            var jobId = queue.Schedule(job, RetryWhenLocked);
            logger.LogInformation("{Puuid} is locked by another job, load-more rescheduled as {JobId}", job.Puuid, jobId);
            return;
        }

        var player = await db.Players.FirstOrDefaultAsync(p => p.Puuid == job.Puuid, ct);
        if (player?.HistoryCoveredUntil is null)
        {
            logger.LogWarning("{Puuid} has not been refreshed yet, nothing to load more from", job.Puuid);
            return;
        }

        IReadOnlyList<string> ingestedIds = [];
        if (!player.HistoryComplete)
        {
            var before = player.HistoryCoveredUntil.Value;
            var page = await ingestor.GetRankedPageAsync(player.Puuid, startTime: null, endTime: before, ct);
            var result = await ingestor.IngestAsync(page.Ids, ct);

            player.ExtendHistory(result.Oldest?.GameStart, complete: !page.HasMore);
            ingestedIds = result.Ingested.Select(m => m.MatchId).ToList();

            logger.LogInformation("{Puuid}: {Ingested} new and {Existing} stored matches before {Before}, complete: {Complete}",
                job.Puuid, result.Ingested.Count, result.Existing.Count, before, player.HistoryComplete);
        }

        // Always published, even for a complete history: the client is waiting to hear back.
        events.Publish(new PlayerRefreshed(player.Puuid, ingestedIds));
        await db.SaveChangesAsync(ct);
    }
}
