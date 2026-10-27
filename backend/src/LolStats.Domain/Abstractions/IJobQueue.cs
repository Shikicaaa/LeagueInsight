using LolStats.Domain.Jobs;

namespace LolStats.Domain.Abstractions;

public interface IJobQueue
{
    string Enqueue(JobRequest job); // returns jobId
    string Schedule(JobRequest job, TimeSpan delay); // for retry after 429
}