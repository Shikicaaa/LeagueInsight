namespace LolStats.Domain.Jobs;

public abstract record JobRequest(JobPriority Priority);

public sealed record RefreshPlayer(string Puuid) : JobRequest(JobPriority.P0);

public sealed record FetchMatches(IReadOnlyList<string> MatchIds, bool WithTimeline, JobPriority Priority) : JobRequest(Priority);

public sealed record LoadMoreMatches(string Puuid, int Start) : JobRequest(JobPriority.P1);
