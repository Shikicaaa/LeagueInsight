namespace LolStats.Domain.Matches;

public interface IMatchStore
{
    Task<IReadOnlySet<string>> GetExistingIdsAsync(IReadOnlySet<string> matchIds, CancellationToken ct = default);
    Task<bool> TryAddAsync(Match match, CancellationToken ct = default);
}