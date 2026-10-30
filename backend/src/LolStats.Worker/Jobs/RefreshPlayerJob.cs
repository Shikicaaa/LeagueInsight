using LolStats.Data;
using LolStats.Domain.Abstractions;
using LolStats.Domain.Events;
using LolStats.Domain.Jobs;
using LolStats.Domain.Players;
using LolStats.Domain.Riot;
using LolStats.Worker.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace LolStats.Worker.Jobs;

// Flow 2: refreshes one player's summoner, rank and new ranked matches.
public sealed class RefreshPlayerJob(
    ILogger<RefreshPlayerJob> logger, IDistributedLock locks,
    IRiotGateway riot, LolStatsDbContext db,
    IEventPublisher events, TimeProvider time, MatchIngestor ingestor) : IJobHandler<RefreshPlayer>
{
    public async Task HandleAsync(RefreshPlayer job, CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync(PlayerLock.Key(job.Puuid), PlayerLock.Ttl, ct);
        if (handle is null)
        {
            logger.LogInformation("{Puuid} is already being refreshed by another worker, skipping", job.Puuid);
            return;
        }

        logger.LogInformation("Refreshing {Puuid} at priority {Priority}", job.Puuid, JobContext.Priority);

        Player? player = await db.Players.FirstOrDefaultAsync(p => p.Puuid == job.Puuid, ct);
        if (player is null)
        {
            var account = await riot.GetAccountByPuuidAsync(job.Puuid, ct);
            if (account is null)
            {
                logger.LogWarning("No account found for {Puuid}, skipping", job.Puuid);
                return;
            }
            player = new Player(account.Puuid, riot.Platform, account.RiotId);
            db.Players.Add(player); // only new players; a loaded one is already tracked and gets an UPDATE
        }

        var summoner = await riot.GetSummonerAsync(player.Puuid, ct);
        if (summoner is null)
        {
            logger.LogWarning("No summoner found for {Puuid}, skipping", job.Puuid);
            return;
        }

        player.UpdateSummoner(summoner.ProfileIconId, summoner.SummonerLevel);

        var league = await riot.GetLeagueEntriesAsync(player.Puuid, ct);
        if (league.Count == 0)
        {
            logger.LogInformation("{Puuid} has no rank entries", job.Puuid);
        }
        foreach (LeagueSnapshot entry in league)
        {
            var previous = await db.LeagueSnapshots
                .Where(s => s.Puuid == player.Puuid && s.Queue == entry.Queue)
                .OrderByDescending(s => s.CapturedAt)
                .FirstOrDefaultAsync(ct);
            if (entry.RankDiffersFrom(previous))
            {
                db.LeagueSnapshots.Add(entry);
                events.Publish(new RankChanged(player.Puuid, entry.Queue, previous?.Tier, entry.Tier, entry.Lp - (previous?.Lp ?? 0)));
            }
        }

        // At most one page, even after a long break: older new matches come with load-more, not in this P0 job.
        var previousLastMatchTs = player.LastMatchTs;
        var page = await ingestor.GetRankedPageAsync(player.Puuid, startTime: previousLastMatchTs, endTime: null, ct);

        logger.LogInformation("{Puuid}: {Total} match ids since {LastMatchTs}, more: {HasMore}",
            job.Puuid, page.Ids.Count, previousLastMatchTs, page.HasMore);
        var result = await ingestor.IngestAsync(page.Ids, ct);

        // Existing matches count too: they may have been ingested through someone else's refresh.
        if (result.Newest is { } newest)
            player.RecordIngestedMatch(newest.MatchId, newest.GameStart);

        if (result.Oldest is { } oldest)
        {
            // The page reaches what we already had, or Riot had nothing more since LastMatchTs.
            var connected = !page.HasMore || oldest.GameStart <= previousLastMatchTs;
            // Only a first refresh (no startTime) that got everything has seen the player's whole history.
            var complete = previousLastMatchTs is null && !page.HasMore;
            player.RecordNewestPage(oldest.GameStart, connected, complete);
        }
        else if (previousLastMatchTs is null)
        {
            player.ExtendHistory(oldest: null, complete: true); // no ranked matches at all
        }

        player.MarkRefreshed(time.GetUtcNow());
        events.Publish(new PlayerRefreshed(player.Puuid, result.Ingested.Select(m => m.MatchId).ToList()));

        await db.SaveChangesAsync(ct);

    }
}
