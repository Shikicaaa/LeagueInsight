namespace LolStats.Domain.Players;

public sealed class LeagueSnapshot
{
    public long Id {get; init;}
    public required string Puuid {get; init;}
    public required string Queue {get; init;} // RANKED_SOLO_5x5
    public required string Tier {get; init;} // DIAMOND
    public required string Division {get; init;} // I
    public required short Lp {get; init;}
    public required int Wins {get; init;}
    public required int Losses {get; init;}
    public required DateTimeOffset CapturedAt {get; init;}

    public bool RankDiffersFrom(LeagueSnapshot? previous) =>
        previous is null
        || previous.Tier != Tier
        || previous.Division != Division
        || previous.Lp != Lp
        || previous.Wins != Wins
        || previous.Losses != Losses;

}