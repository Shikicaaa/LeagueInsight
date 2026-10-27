namespace LolStats.Domain.Abstractions;

public interface ICache
{
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default
    );

    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    ValueTask RemoveAsync(string key, CancellationToken ct = default);
}