using LolStats.Domain.Common;

namespace LolStats.Domain.Players;

public sealed class Player
{
    public string Puuid {get; private set;} = null!;
    public string Platform {get; private set;} = null!;
    public string GameName {get; private set;} = null!;
    public string TagLine {get; private set;} = null!;
    public string RiotIdNormalized {get; private set;} = null!;
    public int? ProfileIconId {get; private set;}
    public int? SummonerLevel {get; private set;}

    public DateTimeOffset? LastMatchTs {get; private set;}
    public DateTimeOffset? LastRefreshedAt {get; private set;}
    public string? DataVersion {get; private set;}

    private Player() {}

    public Player(string puuid, string platform, RiotId riotId)
    {
        Puuid = puuid;
        Platform = platform;
        SetRiotId(riotId);
    }

    public RiotId RiotId => RiotId.Create(GameName, TagLine);
    public void Rename(RiotId riotId) => SetRiotId(riotId);
    public void UpdateSummoner(int profileiconId, int summonerLevel)
    {
        ProfileIconId = profileiconId;
        SummonerLevel = summonerLevel;
    }

    public void MarkRefreshed(DateTimeOffset now) => LastRefreshedAt = now;
    public void RecordIngestedMatch(string matchId, DateTimeOffset gameStart)
    {
        DataVersion = matchId;

        if (LastMatchTs is null || gameStart > LastMatchTs)
            LastMatchTs = gameStart;
    }

    public bool IsStale(DateTimeOffset now, TimeSpan maxAge) =>
        LastRefreshedAt is null || (now - LastRefreshedAt) > maxAge;

    private void SetRiotId(RiotId riotId)
    {
        GameName = riotId.GameName;
        TagLine = riotId.TagLine;
        RiotIdNormalized = riotId.Normalized;
    }
}