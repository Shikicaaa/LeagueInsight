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

    // Every ranked match from LastMatchTs back to HistoryCoveredUntil is in the database.
    // Older matches may exist too (ingested through someone else), but with gaps, so history stops here.
    public DateTimeOffset? HistoryCoveredUntil {get; private set;}
    public bool HistoryComplete {get; private set;} // Riot has nothing older than HistoryCoveredUntil

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
        if (LastMatchTs is not null && gameStart <= LastMatchTs)
            return;

        LastMatchTs = gameStart;
        DataVersion = matchId;
    }

    // Refresh: a page of the newest matches. If it doesn't connect to the history we already had,
    // there may be a gap below it, so coverage starts over from this page and load-more walks back again.
    public void RecordNewestPage(DateTimeOffset oldest, bool connected, bool complete)
    {
        if (connected && HistoryCoveredUntil is not null)
            return;

        HistoryCoveredUntil = oldest;
        HistoryComplete = complete;
    }

    // Load-more: a page older than HistoryCoveredUntil; oldest is null when Riot returned nothing.
    public void ExtendHistory(DateTimeOffset? oldest, bool complete)
    {
        if (oldest is not null && (HistoryCoveredUntil is null || oldest < HistoryCoveredUntil))
            HistoryCoveredUntil = oldest;
        if (complete)
            HistoryComplete = true;
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