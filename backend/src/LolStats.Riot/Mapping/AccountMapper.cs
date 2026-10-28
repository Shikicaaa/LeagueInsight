using LolStats.Domain.Common;
using LolStats.Domain.Riot;
using LolStats.Riot.DTOs;

namespace LolStats.Riot.Mapping;

internal static class AccountMapper
{
    public static RiotAccount? ToDomain(this AccountDto dto) =>
        RiotId.TryCreate(dto.GameName, dto.TagLine, out var riotId)
            ? new RiotAccount(dto.Puuid, riotId) : null;
}