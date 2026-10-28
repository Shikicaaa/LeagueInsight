namespace LolStats.Riot;

internal static class RiotHosts
{
    // summoner-v4, league-v4, champion-mastery-v4, spectator-v5
    public static Uri ForPlatform(string platform) => new($"https://{platform}.api.riotgames.com/");

    public static Uri ForRegion(string region) => new($"https://{region}.api.riotgames.com/");
}