namespace LolStats.Domain.Abstractions;

public interface IDistributedLock
{
    Task<bool> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    
    Task ReleaseAsync(string key, CancellationToken ct = default);
}