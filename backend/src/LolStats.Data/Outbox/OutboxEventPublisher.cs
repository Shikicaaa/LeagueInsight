using LolStats.Domain.Abstractions;
using LolStats.Domain.Events;
using System.Text.Json;

namespace LolStats.Data.Outbox;

internal sealed class OutboxEventPublisher(LolStatsDbContext db, TimeProvider time) : IEventPublisher
{
    public void Publish(IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType();

        db.Outbox.Add(new OutboxMessage
        {
            Type = eventType.Name,
            Payload = JsonSerializer.Serialize(domainEvent, eventType, JsonSerializerOptions.Web),
            CreatedAt = time.GetUtcNow()
        });
    }
}