using System.Text.Json;

namespace LolStats.Riot.DTOs;

public sealed record MatchDto(
    MatchMetadataDto Metadata,
    MatchInfoDto Info
);

public sealed record MatchMetadataDto(
    string DataVersion,
    string MatchId,
    List<string> Participants // PUUIDs, same order as Info.Participants
);

public sealed record MatchInfoDto(
    long GameCreation, // epoch ms
    long GameStartTimestamp, // epoch ms
    long? GameEndTimestamp, // epoch ms
    long GameDuration, // seconds when GameEndTimestamp is present, milliseconds before that
    string GameVersion,
    string GameMode,
    int MapId,
    string PlatformId,
    int QueueId,
    string? EndOfGameResult,
    List<ParticipantDto> Participants,
    List<TeamDto> Teams
);

public sealed record ParticipantDto(
    string Puuid,
    int ParticipantId,
    int TeamId, // 100 blue, 200 red
    string? RiotIdGameName,
    string? RiotIdTagLine,
    int ChampionId,
    string ChampionName,
    int ChampLevel,
    string TeamPosition, // TOP, JUNGLE, MIDDLE, BOTTOM, UTILITY
    string IndividualPosition,
    bool Win,
    bool GameEndedInEarlySurrender, // remake
    int Kills,
    int Deaths,
    int Assists,
    int TotalMinionsKilled,
    int NeutralMinionsKilled, // CS = TotalMinionsKilled + NeutralMinionsKilled
    int GoldEarned,
    int TotalDamageDealtToChampions,
    int TotalDamageTaken,
    int DamageDealtToBuildings,
    int VisionScore,
    int WardsPlaced,
    int WardsKilled,
    int Item0,
    int Item1,
    int Item2,
    int Item3,
    int Item4,
    int Item5,
    int Item6, // trinket
    int Summoner1Id,
    int Summoner2Id,
    PerksDto Perks,
    ChallengesDto? Challenges
);

public sealed record PerksDto(
    PerkStatsDto StatPerks,
    List<PerkStyleDto> Styles
);

public sealed record PerkStatsDto(int Defense, int Flex, int Offense);

public sealed record PerkStyleDto(
    string Description, // primary, sub
    int Style,
    List<PerkSelectionDto> Selections
);

public sealed record PerkSelectionDto(
    int Perk,
    int Var1,
    int Var2,
    int Var3
);

// Riot adds and removes fields here between patches, and not every queue fills them, so all are
// nullable. Only what the analytics plan uses is typed; the rest lands in Extra.
public sealed record ChallengesDto(
    double? Kda,
    double? KillParticipation,
    int? SoloKills,
    int? TurretPlatesTaken,
    int? LaneMinionsFirst10Minutes,
    double? MaxCsAdvantageOnLaneOpponent,
    int? KillsUnderOwnTurret)
{
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; init; }
}

public sealed record TeamDto(
    int TeamId,
    bool Win,
    List<BanDto> Bans,
    ObjectivesDto Objectives
);

public sealed record BanDto(
    int ChampionId,
    int PickTurn
);

public sealed record ObjectivesDto(
    ObjectiveDto? Baron,
    ObjectiveDto? Champion,
    ObjectiveDto? Dragon,
    ObjectiveDto? Horde, // void grubs
    ObjectiveDto? Inhibitor,
    ObjectiveDto? RiftHerald,
    ObjectiveDto? Tower
);

public sealed record ObjectiveDto(
    bool First,
    int Kills
);