using System.Text.Json;
using LolStats.Domain.Matches;
using LolStats.Riot.DTOs;

namespace LolStats.Riot.Mapping;

internal static class MatchMapper
{
    // Maps every match as Riot returned it; deciding what to store (ranked only, no remakes) is up to ingest.
    public static Match ToDomain(this MatchDto dto)
    {
        var info = dto.Info;
        var matchId = dto.Metadata.MatchId;
        var queueId = (QueueId)info.QueueId;
        var gameStart = DateTimeOffset.FromUnixTimeMilliseconds(info.GameStartTimestamp); // UTC, as Npgsql requires

        return new Match
        {
            MatchId = matchId,
            Platform = info.PlatformId.ToLowerInvariant(), // "EUN1" -> "eun1"
            QueueId = queueId,
            Patch = Match.PatchFromGameVersion(info.GameVersion),
            GameStart = gameStart,
            DurationSeconds = DurationSeconds(info),
            WinningTeamId = WinningTeamId(info),
            Participants = info.Participants
                .Select(p => p.ToDomain(matchId, queueId, gameStart))
                .ToList()
        };
    }

    private static MatchParticipant ToDomain(this ParticipantDto p, string matchId, QueueId queueId, DateTimeOffset gameStart) =>
        new()
        {
            MatchId = matchId,
            Puuid = p.Puuid,
            GameStart = gameStart,
            QueueId = queueId,
            ParticipantId = (short)p.ParticipantId,
            TeamId = (short)p.TeamId,
            ChampionId = p.ChampionId,
            TeamPosition = ParsePosition(p.TeamPosition),
            Win = p.Win,
            Kills = (short)p.Kills,
            Deaths = (short)p.Deaths,
            Assists = (short)p.Assists,
            Cs = p.TotalMinionsKilled + p.NeutralMinionsKilled,
            Gold = p.GoldEarned,
            DamageToChampions = p.TotalDamageDealtToChampions,
            DamageToBuildings = p.DamageDealtToBuildings,
            DamageTaken = p.TotalDamageTaken,
            VisionScore = p.VisionScore,
            FinalItems = [p.Item0, p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6],
            SummonerSpells = [p.Summoner1Id, p.Summoner2Id],
            RunesJson = JsonSerializer.Serialize(p.Perks, JsonSerializerOptions.Web)
            // PerfScore, TeamRank, GameRank: computed by Analytics (phase 2).
        };

    // Before patch 11.20 GameDuration was in milliseconds; GameEndTimestamp only exists since then.
    private static int DurationSeconds(MatchInfoDto info) =>
        info.GameEndTimestamp is null
            ? (int)(info.GameDuration / 1000)
            : (int)info.GameDuration;

    // 0 when no team is marked as the winner (can happen on remakes).
    private static short WinningTeamId(MatchInfoDto info) =>
        (short)(info.Teams.FirstOrDefault(t => t.Win)?.TeamId ?? 0);

    // Explicit, so renaming an enum member can't silently break parsing of Riot's strings.
    private static TeamPosition ParsePosition(string position) => position switch
    {
        "TOP" => TeamPosition.TOP,
        "JUNGLE" => TeamPosition.JUNGLE,
        "MIDDLE" => TeamPosition.MIDDLE,
        "BOTTOM" => TeamPosition.BOTTOM,
        "UTILITY" => TeamPosition.UTILITY,
        _ => TeamPosition.UNKNOWN
    };
}
