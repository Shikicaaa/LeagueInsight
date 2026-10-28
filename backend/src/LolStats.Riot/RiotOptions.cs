namespace LolStats.Riot;

public sealed class RiotOptions
{
    public const string SectionName = "Riot";
    
    public string ApiKey {get;set;} = "";
    public string Platform {get;set;} = ""; // "eun1"
    public string Region {get;set;} = ""; // "europe"

    public string DefaultAppRateLimit {get;set;} = "20:1, 100:120";
    public TimeSpan MaxInProcessWait {get;set;} = TimeSpan.FromSeconds(3);
}