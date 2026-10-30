using LolStats.Domain.Jobs;
using LolStats.Worker.Ingestion;

namespace LolStats.Worker.Jobs;

// Flow 5: fetches matches not in the database yet (match details opened by link, batches).
// No lock: it only inserts immutable matches (ON CONFLICT DO NOTHING) and never touches a Player row.
public sealed class FetchMatchesJob(ILogger<FetchMatchesJob> logger, MatchIngestor ingestor) : IJobHandler<FetchMatches>
{
    public async Task HandleAsync(FetchMatches job, CancellationToken ct)
    {
        var result = await ingestor.IngestAsync(job.MatchIds.ToHashSet(), ct);

        logger.LogInformation("Fetched {Count} of {Requested} matches, {Existing} already stored",
            result.Ingested.Count, job.MatchIds.Count, result.Existing.Count);
    }
}
