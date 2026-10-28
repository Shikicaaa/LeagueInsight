using LolStats.Domain.Common;
using LolStats.Domain.Riot;
using LolStats.Riot.DTOs;

namespace LolStats.Riot.Mapping;

internal static class SummonerMapper
{
    public static SummonerProfile ToDomain(this SummonerDto dto) =>
        new (dto.ProfileIconId, (int)dto.SummonerLevel);
}