namespace LolStats.Riot.DTOs;

public sealed record LeagueEntryDto(
    string LeagueId,
    string Puuid,
    string QueueType,
    string Tier,
    string Rank,
    int LeaguePoints,
    int Wins,
    int Losses,
    bool HotStreak,
    bool Veteran,
    bool FreshBlood,
    bool Inactive
);