using LolStats.Data;
using LolStats.Domain.Abstractions;
using LolStats.Domain.Events;
using LolStats.Domain.Jobs;
using LolStats.Domain.Matches;
using LolStats.Domain.Players;
using LolStats.Domain.Riot;
using Microsoft.EntityFrameworkCore;

namespace LolStats.Worker.Jobs;

// Flow 2: refreshes one player's summoner, rank and new ranked matches.
public sealed class RefreshPlayerJob(
    ILogger<RefreshPlayerJob> logger, IDistributedLock locks,
    IRiotGateway riot, LolStatsDbContext db, IMatchStore matches,
    IEventPublisher events, TimeProvider time) : IJobHandler<RefreshPlayer>
{
    public async Task HandleAsync(RefreshPlayer job, CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync($"lock:job:{job.Puuid}", TimeSpan.FromMinutes(4), ct);
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

        var ids = new HashSet<string>();
        foreach (var queue in new[] { QueueId.RankedSolo, QueueId.RankedFlex })
        {
            var queueIds = await riot.GetMatchIdsAsync(player.Puuid, new MatchIdsQuery(queue, StartTime: player.LastMatchTs), ct);
            ids.UnionWith(queueIds);
        }

        var existing = await matches.GetExistingIdsAsync(ids, ct);
        if (existing.Count > 0)
        {
            var latestExisting = await db.Matches
                .Where(m => existing.Contains(m.MatchId))
                .OrderByDescending(m => m.GameStart)
                .Select(m => new { m.MatchId, m.GameStart })
                .FirstOrDefaultAsync(ct);
            if (latestExisting is not null)
                player.RecordIngestedMatch(latestExisting.MatchId, latestExisting.GameStart);    
        }
        
        var newIds = ids.Except(existing)
            .OrderBy(id => long.Parse(id[(id.IndexOf('_') + 1)..]))   // EUN1_4021457560 -> 4021457560, oldest first
            .ToList();

        logger.LogInformation("{Puuid}: {Total} match ids since {LastMatchTs}", player.Puuid, ids.Count, player.LastMatchTs);

        var savedIds = new List<string>();
        foreach (var matchId in newIds)
        {
            var match = await riot.GetMatchAsync(matchId, ct);
            if (match is null)
            {
                logger.LogWarning("No match found for {MatchId}, skipping", matchId);
                continue;
            }

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (await matches.TryAddAsync(match, ct))
            {
                events.Publish(new MatchIngested(
                    match.MatchId, 
                    match.Participants.Select(p => p.Puuid).ToList(), 
                    match.Patch, 
                    match.QueueId, 
                    match.HasTimeline
                ));
            }

            player.RecordIngestedMatch(match.MatchId, match.GameStart);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            savedIds.Add(match.MatchId);
        }

        player.MarkRefreshed(time.GetUtcNow());
        events.Publish(new PlayerRefreshed(player.Puuid, savedIds));

        await db.SaveChangesAsync(ct);

    }
}
