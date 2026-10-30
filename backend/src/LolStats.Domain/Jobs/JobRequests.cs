namespace LolStats.Domain.Jobs;

public abstract record JobRequest(JobPriority Priority);

public sealed record RefreshPlayer(string Puuid) : JobRequest(JobPriority.P0);

public sealed record FetchMatches(IReadOnlyList<string> MatchIds, bool WithTimeline, JobPriority Priority) : JobRequest(Priority);

// No cursor: the job reads Player.HistoryCoveredUntil, so the client can't skip over a gap.
public sealed record LoadMoreMatches(string Puuid) : JobRequest(JobPriority.P1);
