using LolStats.Domain.Jobs;

namespace LolStats.Infrastructure.Jobs;

// Hangfire queue names must be lowercase. Hangfire.PostgreSql fetches ORDER BY queue name,
// so p0 < p1 < p2 < p3 is what makes P0 jobs go first. Keep the names sortable.
public static class JobQueues
{
    public const string P0 = "p0";
    public const string P1 = "p1";
    public const string P2 = "p2";
    public const string P3 = "p3";

    public static readonly string[] All = [P0, P1, P2, P3];

    public static string For(JobPriority priority) => priority switch
    {
        JobPriority.P0 => P0,
        JobPriority.P1 => P1,
        JobPriority.P2 => P2,
        JobPriority.P3 => P3,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
    };
}
