using LolStats.Domain.Common;
using LolStats.Domain.Matches;
using LolStats.Domain.Players;
using LolStats.Domain.Riot;
using LolStats.Riot.Clients;
using LolStats.Riot.Mapping;
using Microsoft.Extensions.Options;

namespace LolStats.Riot;

internal sealed class RiotGateway(
    AccountClient accounts,
    SummonerClient summoners,
    LeagueClient leagues,
    MatchClient matchClient,
    IOptions<RiotOptions> options,
    TimeProvider time) : IRiotGateway
{
    public string Platform => options.Value.Platform;

    public async Task<RiotAccount?> GetAccountByRiotIdAsync(RiotId riotId, CancellationToken ct = default)
    {
        var dto = await accounts.GetByRiotIdAsync(riotId.GameName, riotId.TagLine, ct);
        return dto?.ToDomain();
    }
    public async Task<RiotAccount?> GetAccountByPuuidAsync(string puuid, CancellationToken ct = default)
    {
        var dto = await accounts.GetByPuuidAsync(puuid, ct);
        return dto?.ToDomain();
    }

    public async Task<SummonerProfile?> GetSummonerAsync(string puuid, CancellationToken ct = default)
    {
        var dto = await summoners.GetSummonerByPuuidAsync(puuid, ct);
        return dto?.ToDomain();
    }

    public async Task<IReadOnlyList<LeagueSnapshot>> GetLeagueEntriesAsync(string puuid, CancellationToken ct = default)
    {
        var dtos = await leagues.GetLeagueEntriesByPuuidAsync(puuid, ct);
        if (dtos is null)
            return [];

        // One timestamp for the whole response, so Solo and Flex snapshots line up.
        var now = time.GetUtcNow();
        return dtos
            .Where(dto => LeagueMapper.TrackedQueues.Contains(dto.QueueType))
            .Select(dto => dto.ToDomain(now))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetMatchIdsAsync(string puuid, MatchIdsQuery query, CancellationToken ct = default)
    {
        var ids = await matchClient.GetMatchesByPuuidAsync(
            puuid,
            (int)query.Queue,
            query.StartTime?.ToUnixTimeSeconds(),
            query.Start,
            query.Count,
            ct);
        return ids ?? [];
    }

    public async Task<Match?> GetMatchAsync(string matchId, CancellationToken ct = default)
    {
        var dto = await matchClient.GetMatchByIdAsync(matchId, ct);
        return dto?.ToDomain();
    }
}