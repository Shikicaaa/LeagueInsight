namespace LolStats.Riot.RateLimiting;

public sealed record RateLimitWindow(int Limit, TimeSpan Length, int Count = 0);

internal static class RateLimitWindows
{
    public static IReadOnlyList<RateLimitWindow> Parse(HttpResponseMessage response, string header)
    {
        if (!response.Headers.TryGetValues(header, out var limits))
            return [];
        
        var counts = response.Headers.TryGetValues($"{header}-Count", out var c)
            ? ParsePairs(c.First()).ToDictionary(p => p.Seconds, p => p.Value) : [];
        
        return ParsePairs(limits.First())
            .Select(p => new RateLimitWindow(p.Value, TimeSpan.FromSeconds(p.Seconds), counts.GetValueOrDefault(p.Seconds))).ToList();
    }

    public static IReadOnlyList<RateLimitWindow> Parse(string value) =>
        ParsePairs(value).Select(p => new RateLimitWindow(p.Value, TimeSpan.FromSeconds(p.Seconds))).ToList();

    private static IEnumerable<(int Value, int Seconds)> ParsePairs(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair => pair.Split(':'))
            .Select(parts => (int.Parse(parts[0]), int.Parse(parts[1])));
}