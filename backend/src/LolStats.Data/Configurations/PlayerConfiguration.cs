using LolStats.Domain.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LolStats.Data.Configurations;

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Puuid);

        builder.Property(p => p.Platform).IsRequired();
        builder.Property(p => p.GameName).IsRequired();
        builder.Property(p => p.TagLine).IsRequired();
        builder.Property(p => p.RiotIdNormalized).IsRequired();

        builder.HasIndex(p => new { p.Platform, p.RiotIdNormalized}).IsUnique();

        builder.Ignore(p => p.RiotId);
    }
}