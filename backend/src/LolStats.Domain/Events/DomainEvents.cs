using LolStats.Domain.Matches;

namespace LolStats.Domain.Events;

public sealed record MatchIngested(
    string MatchId,
    IReadOnlyList<string> Puuids,
    string Patch,
    QueueId QueueId,
    bool HasTimeline) : IDomainEvent;

public sealed record PlayerRefreshed(
    string Puuid,
    IReadOnlyList<string> NewMatchIds) : IDomainEvent;

public sealed record RankChanged(
    string Puuid,
    string Queue,
    string? OldTier,
    string NewTier,
    int LpDelta
) : IDomainEvent;