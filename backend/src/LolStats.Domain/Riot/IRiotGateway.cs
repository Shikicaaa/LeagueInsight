using LolStats.Domain.Common;
using LolStats.Domain.Matches;
using LolStats.Domain.Players;

namespace LolStats.Domain.Riot;

public interface IRiotGateway
{
    // Platform gateway is talking to, stored on every player and match
    string Platform {get;}

    Task<RiotAccount?> GetAccountByRiotIdAsync(RiotId riotId, CancellationToken ct = default);

    Task<RiotAccount?> GetAccountByPuuidAsync(string puuid, CancellationToken ct = default);

    Task<SummonerProfile?> GetSummonerAsync(string puuid, CancellationToken ct = default);

    Task<IReadOnlyList<LeagueSnapshot>> GetLeagueEntriesAsync(string puuid, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetMatchIdsAsync(string puuid, MatchIdsQuery query, CancellationToken ct = default);

    Task<Match?> GetMatchAsync(string matchId, CancellationToken ct = default);
}