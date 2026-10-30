using LolStats.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace LolStats.Data.Matches;

internal sealed class MatchStore(LolStatsDbContext db) : IMatchStore
{
    public async Task<IReadOnlyDictionary<string, DateTimeOffset>> GetGameStartsAsync(IReadOnlySet<string> matchIds, CancellationToken ct = default)
    {
        return await db.Matches
            .Where(m => matchIds.Contains(m.MatchId))
            .Select(m => new { m.MatchId, m.GameStart })   // project first, or EF loads and tracks whole matches
            .ToDictionaryAsync(m => m.MatchId, m => m.GameStart, ct);
    }

    // Inserts the match row immediately, participants are only tracked.
    // Must run inside a transaction together with SaveChanges, or a failure leaves a match without participants.
    public async Task<bool> TryAddAsync(Match match, CancellationToken ct = default)
    {
        var result = await db.Database.ExecuteSqlAsync($"""
            INSERT INTO matches (
                match_id, platform, queue_id,
                patch, game_start, duration_seconds,
                winning_team_id, has_timeline, raw_ref)
            VALUES (
                {match.MatchId}, {match.Platform}, {match.QueueId},
                {match.Patch}, {match.GameStart}, {match.DurationSeconds},
                {match.WinningTeamId}, {match.HasTimeline}, {match.RawRef})
            ON CONFLICT (match_id) DO NOTHING
        """, ct);
        if (result == 0)
        {
            return false;
        }

        db.MatchParticipants.AddRange(match.Participants);
        return true;
    }
}