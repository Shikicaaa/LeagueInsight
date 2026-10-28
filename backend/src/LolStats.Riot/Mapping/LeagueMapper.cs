using LolStats.Domain.Players;
using LolStats.Riot.DTOs;

namespace LolStats.Riot.Mapping;

internal static class LeagueMapper
{
    // Only the queues we track; Riot may return others.
    public static readonly HashSet<string> TrackedQueues = ["RANKED_SOLO_5x5", "RANKED_FLEX_SR"];

    // Id is left unset: the database assigns it (identity column).
    public static LeagueSnapshot ToDomain(this LeagueEntryDto dto, DateTimeOffset capturedAt) =>
        new()
        {
            Puuid = dto.Puuid,
            Queue = dto.QueueType,
            Tier = dto.Tier,
            Division = dto.Rank,
            Lp = (short)dto.LeaguePoints,
            Wins = dto.Wins,
            Losses = dto.Losses,
            CapturedAt = capturedAt
        };
}
