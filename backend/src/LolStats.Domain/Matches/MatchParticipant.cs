namespace LolStats.Domain.Matches;

public sealed class MatchParticipant
{
    public required string MatchId {get; init; }
    public required string Puuid { get; init;}
    public required short ParticipantId {get; init;}
    public required short TeamId {get; init;}
    public required int ChampionId {get; init;}
    public required QueueId QueueId { get; init; }
    public required DateTimeOffset GameStart {get; init;}
    public required TeamPosition TeamPosition {get; init;}
    public required bool Win {get; init;}

    public short Kills {get; init;}
    public short Deaths {get; init;}
    public short Assists {get; init;}
    public int Cs {get; init;}
    public int Gold {get; init;}
    public int DamageToChampions {get; init;}
    public int DamageToBuildings {get; init;}
    public int DamageTaken {get; init;}
    public int VisionScore {get; init;}

    public int[] FinalItems {get; init;} = Array.Empty<int>();
    public int[] SummonerSpells {get; init;} = Array.Empty<int>();
    public string? RunesJson {get; init;}

    public float PerfScore {get; init;}
    public short TeamRank {get; init;}
    public short GameRank {get; init;}

    public double Kda => (Kills + Assists) / (double)Math.Max(1, (int)Deaths);
    
}