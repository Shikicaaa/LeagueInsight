namespace LolStats.Domain.Matches;

public interface IMatchStore
{
    // Matches already in the database, with their start time (callers need it to track history coverage).
    Task<IReadOnlyDictionary<string, DateTimeOffset>> GetGameStartsAsync(IReadOnlySet<string> matchIds, CancellationToken ct = default);
    Task<bool> TryAddAsync(Match match, CancellationToken ct = default);
}
