namespace LolStats.Domain.Jobs;

public static class JobContext
{
    private static readonly AsyncLocal<JobPriority?> Current = new();

    public static JobPriority Priority => Current.Value ?? JobPriority.P0;

    public static IDisposable Begin(JobPriority priority)
    {
        var previous = Current.Value;
        Current.Value = priority;
        return new Scope(previous);
    }

    private sealed class Scope(JobPriority? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}