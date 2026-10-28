using LolStats.Riot.DTOs;

namespace LolStats.Riot.Clients;

internal sealed class MatchClient(HttpClient http) : RiotClientBase(http)
{
    public Task<MatchDto?> GetMatchByIdAsync(string matchId, CancellationToken ct)
    {
        return GetAsync<MatchDto>(
            $"lol/match/v5/matches/{Uri.EscapeDataString(matchId)}",
            "match-v5.by-match-id",
            ct
        );
    }
    
    // Riot's /ids endpoint: newest first, one queue per call, count capped at 100.
    public Task<IReadOnlyList<string>?> GetMatchesByPuuidAsync(
        string puuid,
        int queue,
        long? startTimeSeconds,   // epoch SECONDS, not ms
        int start = 0,
        int count = 20,
        CancellationToken ct = default
    )
    {
        var path = $"lol/match/v5/matches/by-puuid/{Uri.EscapeDataString(puuid)}/ids" +
                   $"?queue={queue}&start={start}&count={Math.Clamp(count, 1, 100)}";

        if (startTimeSeconds is not null)
            path += $"&startTime={startTimeSeconds}";

        return GetAsync<IReadOnlyList<string>>(path, "match-v5.by-puuid", ct);
    }
}