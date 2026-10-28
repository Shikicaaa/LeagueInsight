using LolStats.Riot.DTOs;

namespace LolStats.Riot.Clients;

internal sealed class LeagueClient(HttpClient http) : RiotClientBase(http)
{
    public Task<IReadOnlyList<LeagueEntryDto>?> GetLeagueEntriesByPuuidAsync(string puuid, CancellationToken ct)
    {
        return GetAsync<IReadOnlyList<LeagueEntryDto>>(
            $"lol/league/v4/entries/by-puuid/{Uri.EscapeDataString(puuid)}",
            "league-v4.by-puuid",
            ct
        );
    }
}