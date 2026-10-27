namespace LolStats.Data.Outbox;

public sealed class OutboxMessage
{
    public long Id { get; init; }
    public required string Type { get; init; } // "MatchIngested"
    public required string Payload {get; init; } // JSON
    public required DateTimeOffset CreatedAt {get; init; }
    public DateTimeOffset? PublishedAt {get; set; } // null until OutboxDispatcher handles it
}