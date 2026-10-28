using LolStats.Domain.Abstractions;
using StackExchange.Redis;

namespace LolStats.Infrastructure.Locking;

internal sealed class ValkeyDistributedLock(IConnectionMultiplexer valkey) : IDistributedLock
{
    private const string ReleaseScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        end
        return 0
    """;

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var db = valkey.GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        var acquired = await db.StringSetAsync(key, token, expiry, When.NotExists);
        return acquired ? new Handle(db,key,token) : null;    
    }

    private sealed class Handle(IDatabase db, string key, string token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await db.ScriptEvaluateAsync(ReleaseScript, [key], [token]);
        }
    }
}