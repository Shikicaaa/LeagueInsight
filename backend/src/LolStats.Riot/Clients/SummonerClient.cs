using LolStats.Riot.DTOs;

namespace LolStats.Riot.Clients;

internal sealed class SummonerClient(HttpClient http) : RiotClientBase(http)
{
    public Task<SummonerDto?> GetSummonerByPuuidAsync(string puuid, CancellationToken ct)
    {
        return GetAsync<SummonerDto>(
            $"lol/summoner/v4/summoners/by-puuid/{Uri.EscapeDataString(puuid)}",
            "summoner-v4.by-puuid",
            ct
        );
    }
}