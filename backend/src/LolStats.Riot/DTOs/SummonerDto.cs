namespace LolStats.Riot.DTOs;
public sealed record SummonerDto(
    string Puuid,
    int ProfileIconId,
    long RevisionDate, // epoch ms
    long SummonerLevel
);