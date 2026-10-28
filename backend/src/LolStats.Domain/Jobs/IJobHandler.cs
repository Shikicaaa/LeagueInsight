namespace LolStats.Domain.Jobs;

// One handler per JobRequest type, implemented in Worker. The queue only knows the request;
// whoever runs it resolves the matching handler.
public interface IJobHandler<in TJob> where TJob : JobRequest
{
    Task HandleAsync(TJob job, CancellationToken ct);
}
