using LolStats.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LolStats.Data.Configurations;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.MatchId);

        builder.Property(m => m.Platform).IsRequired();
        builder.Property(m => m.Patch).IsRequired();
        builder.Property(m => m.QueueId).HasConversion<int>();

        builder.HasMany(m => m.Participants)
            .WithOne()
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Derived from Participants, not columns.
        builder.Ignore(m => m.ParticipantCount);
        builder.Ignore(m => m.Puuids);
    }
}