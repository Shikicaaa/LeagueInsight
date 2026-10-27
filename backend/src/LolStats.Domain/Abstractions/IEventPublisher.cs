using LolStats.Domain.Events;

namespace LolStats.Domain.Abstractions;

public interface IEventPublisher
{
    void Publish(IDomainEvent domainEvent);
}