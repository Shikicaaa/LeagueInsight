using Hangfire;
using LolStats.Domain.Abstractions;
using LolStats.Domain.Jobs;

namespace LolStats.Infrastructure.Jobs;

internal sealed class HangfireJobQueue(IBackgroundJobClient client) : IJobQueue
{
    public string Enqueue(JobRequest job) => Dispatch(job, delay: null);

    public string Schedule(JobRequest job, TimeSpan delay) => Dispatch(job, delay);

    // Hangfire needs the concrete type to build JobRunner<T>, so every new JobRequest gets an arm here.
    private string Dispatch(JobRequest job, TimeSpan? delay) => job switch
    {
        RefreshPlayer j => Send(j, delay),
        FetchMatches j => Send(j, delay),
        LoadMoreMatches j => Send(j, delay),
        _ => throw new NotSupportedException($"No Hangfire mapping for {job.GetType().Name}.")
    };

    private string Send<TJob>(TJob job, TimeSpan? delay) where TJob : JobRequest
    {
        var queue = JobQueues.For(job.Priority);

        // CancellationToken.None is a placeholder; Hangfire passes its own token when the job runs.
        return delay is null
            ? client.Enqueue<JobRunner<TJob>>(queue, r => r.RunAsync(job, CancellationToken.None))
            : client.Schedule<JobRunner<TJob>>(queue, r => r.RunAsync(job, CancellationToken.None), delay.Value);
    }
}
