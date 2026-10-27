using LolStats.Domain.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LolStats.Data.Configurations;
internal sealed class LeagueSnapshotConfiguration : IEntityTypeConfiguration<LeagueSnapshot>
{
    public void Configure(EntityTypeBuilder<LeagueSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityAlwaysColumn();

        builder.Property(s => s.Queue).IsRequired();
        builder.Property(s => s.Tier).IsRequired();
        builder.Property(s => s.Division).IsRequired();

        builder.HasIndex(s => new { s.Puuid, s.Queue, s.CapturedAt })
            .IsDescending(false, false, true);
    }
}