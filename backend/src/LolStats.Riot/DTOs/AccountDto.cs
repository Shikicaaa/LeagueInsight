namespace LolStats.Riot.DTOs;

public sealed record AccountDto(
    string Puuid,
    string? GameName,
    string? TagLine
);