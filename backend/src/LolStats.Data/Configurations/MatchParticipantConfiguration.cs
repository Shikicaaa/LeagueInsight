using LolStats.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LolStats.Data.Configurations;

internal sealed class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> builder)
    {
        builder.HasKey(p => new { p.MatchId, p.Puuid });

        builder.Property(p => p.QueueId).HasConversion<int>();
        builder.Property(p => p.TeamPosition).HasConversion<short>();
        builder.Property(p => p.RunesJson).HasColumnType("jsonb");

        // Match history: WHERE puuid = @p AND (game_start, match_id) < @cursor ORDER BY game_start DESC LIMIT 10
        builder.HasIndex(p => new { p.Puuid, p.GameStart, p.MatchId })
            .IsDescending(false, true, true);

        // Champion stats and "who mains champion X".
        builder.HasIndex(p => new { p.ChampionId, p.Puuid });

        builder.Ignore(p => p.Kda);
    }
}