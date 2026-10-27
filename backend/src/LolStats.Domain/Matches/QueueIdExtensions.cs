namespace LolStats.Domain.Matches;

public static class QueueIdExtensions
{
    public static bool IsRanked(this QueueId queue) => queue is QueueId.RankedSolo or QueueId.RankedFlex;

    public static bool IsSummonersRift5v5(this QueueId queue) =>
        queue is QueueId.RankedSolo or QueueId.RankedFlex
        or QueueId.NormalDraft or QueueId.NormalBlind or QueueId.Swiftplay
        or QueueId.Clash or QueueId.Quickplay;

    public static bool IsAram(this QueueId queue) => queue is QueueId.Aram or QueueId.AramMayhem;

    public static bool IsArena(this QueueId queue) => queue is QueueId.Arena or QueueId.AramMayhem;

    public static int? ExpectedParticipantCount(this QueueId queue) => queue switch
    {
        QueueId.RankedSolo or QueueId.RankedFlex => 10,
        _ => null
    };
}