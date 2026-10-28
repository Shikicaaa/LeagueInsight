using System.Collections.Concurrent;
using LolStats.Domain.Jobs;
using Microsoft.Extensions.Options;

namespace LolStats.Riot.RateLimiting;

internal sealed class InMemoryRateLimiter(TimeProvider time, IOptions<RiotOptions> options) : IRateLimiter
{
    // P2/P3 only run while more than 30% of the app budget is left, so they never slow down users.
    private const double BackgroundReserve = 0.3;

    private readonly ConcurrentDictionary<string, Bucket> _buckets = new();

    public TimeSpan? TryAcquire(string appKey, string methodKey, JobPriority priority)
    {
        var app = _buckets.GetOrAdd(appKey, _ => new Bucket(RateLimitWindows.Parse(options.Value.DefaultAppRateLimit)));
        // Method limits are unknown until Riot's first response; no windows = no method limit yet.
        var method = _buckets.GetOrAdd(methodKey, _ => new Bucket([]));
        var reserve = priority >= JobPriority.P2 ? BackgroundReserve : 0;
        var now = time.GetUtcNow();

        // Always lock app before method so two threads can't deadlock.
        lock (app)
        lock (method)
        {
            var wait = Max(app.TimeUntilFree(now, reserve), method.TimeUntilFree(now, 0));
            if (wait > TimeSpan.Zero)
                return wait;

            app.Take(now);
            method.Take(now);
            return null;
        }
    }

    public void Sync(string key, IReadOnlyList<RateLimitWindow> windows)
    {
        if (windows.Count == 0)
            return;

        var bucket = _buckets.GetOrAdd(key, _ => new Bucket([]));
        lock (bucket)
            bucket.Sync(windows, time.GetUtcNow());
    }

    public void Pause(string key, TimeSpan retryAfter)
    {
        var bucket = _buckets.GetOrAdd(key, _ => new Bucket([]));
        lock (bucket)
            bucket.PausedUntil = time.GetUtcNow() + retryAfter;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    // Fixed windows, corrected by Riot's own counts after every response.
    private sealed class Bucket(IReadOnlyList<RateLimitWindow> windows)
    {
        private List<Window> _windows = windows.Select(w => new Window(w.Limit, w.Length)).ToList();

        public DateTimeOffset PausedUntil { get; set; }

        public TimeSpan TimeUntilFree(DateTimeOffset now, double reserve)
        {
            var wait = PausedUntil > now ? PausedUntil - now : TimeSpan.Zero;

            foreach (var window in _windows)
            {
                window.ResetIfExpired(now);
                var allowed = (int)(window.Limit * (1 - reserve));
                if (window.Count >= allowed)
                    wait = Max(wait, window.Start + window.Length - now);
            }

            return wait;
        }

        public void Take(DateTimeOffset now)
        {
            foreach (var window in _windows)
            {
                window.ResetIfExpired(now);
                window.Count++;
            }
        }

        public void Sync(IReadOnlyList<RateLimitWindow> reported, DateTimeOffset now)
        {
            var current = _windows.ToDictionary(w => w.Length);
            _windows = reported.Select(r =>
            {
                var window = current.GetValueOrDefault(r.Length) ?? new Window(r.Limit, r.Length) { Start = now };
                window.Limit = r.Limit;
                // Riot's count includes calls from other processes sharing the key; trust the higher one.
                window.Count = Math.Max(window.Count, r.Count);
                return window;
            }).ToList();
        }
    }

    private sealed class Window(int limit, TimeSpan length)
    {
        public int Limit { get; set; } = limit;
        public TimeSpan Length { get; } = length;
        public DateTimeOffset Start { get; set; }
        public int Count { get; set; }

        public void ResetIfExpired(DateTimeOffset now)
        {
            if (now < Start + Length)
                return;
            Start = now;
            Count = 0;
        }
    }
}