namespace LolStats.Domain.Abstractions;

public interface IDistributedLock
{        
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken ct = default);
}