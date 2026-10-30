namespace LolStats.Worker.Jobs;

// One key for every job that writes a Player row (refresh, load-more), so they never run at once for the same player.
internal static class PlayerLock
{
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(4);

    public static string Key(string puuid) => $"lock:job:{puuid}";
}
