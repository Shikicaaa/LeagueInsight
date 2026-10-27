namespace LolStats.Domain.Matches;

public sealed class Match
{
    public int ParticipantCount => Participants.Count;
    
    public required string MatchId {get; init;}
    public required string Platform {get; init;}
    public required QueueId QueueId {get; init;}
    public required string Patch {get; init;}
    public required DateTimeOffset GameStart {get; init;}
    public required int DurationSeconds {get; init;}
    public required short WinningTeamId {get; init;}
    
    public bool HasTimeline {get; private set;}
    public string? RawRef {get; private set;} // compressed JSON in S3 or Minio

    public List<MatchParticipant> Participants {get; init;} = [];
    public IEnumerable<string> Puuids => Participants.Select(p => p.Puuid);

    public void AttachTimeline(string rawRef)
    {
        this.HasTimeline = true;
        this.RawRef = rawRef;
    }

    public void SetRawRef(string rawRef) => RawRef = rawRef;

    public static string PatchFromGameVersion(string gameVersion)
    {
        var parts = gameVersion.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : gameVersion;
    }
}