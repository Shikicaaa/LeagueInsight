using LolStats.Data.Outbox;
using LolStats.Domain.Matches;
using LolStats.Domain.Players;
using Microsoft.EntityFrameworkCore;

namespace LolStats.Data;

public sealed class LolStatsDbContext(DbContextOptions<LolStatsDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();
    public DbSet<LeagueSnapshot> LeagueSnapshots => Set<LeagueSnapshot>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LolStatsDbContext).Assembly);
    }
}