using LolStats.Domain.Common;
using LolStats.Domain.Matches;

namespace LolStats.Domain.Riot;

public sealed record RiotAccount(string Puuid, RiotId RiotId);

public sealed record SummonerProfile(int ProfileIconId, int SummonerLevel);

public sealed record MatchIdsQuery(
    QueueId Queue,
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null,
    int Start = 0,
    int Count = 10
);
