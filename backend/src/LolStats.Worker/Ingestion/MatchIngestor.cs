using LolStats.Data;
using LolStats.Domain.Abstractions;
using LolStats.Domain.Matches;
using LolStats.Domain.Riot;
using LolStats.Domain.Events;

namespace LolStats.Worker.Ingestion;

// HasMore: Riot may have more matches in the window than this page holds.
public sealed record RankedPage(IReadOnlySet<string> Ids, bool HasMore);

public sealed record StoredMatch(string MatchId, DateTimeOffset GameStart);

public sealed record IngestResult(IReadOnlyDictionary<string, DateTimeOffset> Existing, IReadOnlyList<Match> Ingested)
{
    // Requested matches now in the database (404s are missing); null when there are none.
    public StoredMatch? Newest => Stored().MaxBy(m => m.GameStart);
    public StoredMatch? Oldest => Stored().MinBy(m => m.GameStart);

    private IEnumerable<StoredMatch> Stored() =>
        Existing.Select(e => new StoredMatch(e.Key, e.Value))
            .Concat(Ingested.Select(m => new StoredMatch(m.MatchId, m.GameStart)));
}

public sealed class MatchIngestor(IMatchStore matches, IRiotGateway riot, IEventPublisher events, LolStatsDbContext db, ILogger<MatchIngestor> logger)
{
    public const int PageSize = 10;

    private static readonly QueueId[] RankedQueues = [QueueId.RankedSolo, QueueId.RankedFlex];

    // Newest PageSize ranked ids in the window, both queues merged. Taking the newest of the union keeps the
    // page gap-free: any match newer than the page's oldest is within its own queue's top PageSize too.
    public async Task<RankedPage> GetRankedPageAsync(
        string puuid, DateTimeOffset? startTime, DateTimeOffset? endTime, CancellationToken ct)
    {
        var ids = new List<string>();
        var hasMore = false;
        foreach (var queue in RankedQueues)
        {
            var queueIds = await riot.GetMatchIdsAsync(puuid, new MatchIdsQuery(queue, startTime, endTime, Count: PageSize), ct);
            ids.AddRange(queueIds);
            hasMore |= queueIds.Count == PageSize;
        }

        var page = ids.OrderByDescending(Sequence).Take(PageSize).ToHashSet();
        return new RankedPage(page, hasMore || ids.Count > PageSize);
    }

    // Diff with the database, fetch only missing ids, oldest first, one transaction per match.
    // Callers derive player progress from the result afterwards, so a crash mid-way is simply resumed by the next run.
    public async Task<IngestResult> IngestAsync(IReadOnlySet<string> ids, CancellationToken ct)
    {
        var existing = await matches.GetGameStartsAsync(ids, ct);
        var newIds = ids.Where(id => !existing.ContainsKey(id))
            .OrderBy(Sequence)
            .ToList();

        var ingested = new List<Match>();
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
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            ingested.Add(match);
        }
        return new IngestResult(existing, ingested);
    }

    // EUN1_4021457560 -> 4021457560; ids grow with game creation, so this orders matches by time.
    public static long Sequence(string matchId) => long.Parse(matchId[(matchId.IndexOf('_') + 1)..]);
}
