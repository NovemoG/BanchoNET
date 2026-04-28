using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class ScoreConfiguration : IEntityTypeConfiguration<ScoreDto>
{
    public void Configure(
        EntityTypeBuilder<ScoreDto> builder
    ) {
        builder.ToTable("Scores");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.PP);
        builder.HasIndex(s => s.LegacyTotalScore);
        builder.HasIndex(s => s.Mods);
        builder.HasIndex(s => s.BeatmapMD5);
        builder.HasIndex(s => s.MapId);
        builder.HasIndex(s => s.OnlineChecksum);
        builder.HasIndex(s => s.Mode);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.PlayTime);
        builder.HasIndex(s => s.IsRestricted);

        builder.Property(s => s.BeatmapMD5)
            .IsRequired()
            .HasColumnType("CHAR(32)")
            .IsUnicode(false);

        builder.Property(s => s.OnlineChecksum)
            .HasColumnType("CHAR(32)")
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.Property(s => s.PP)
            .HasColumnType("numeric(7,3)");

        builder.Property(s => s.Acc)
            .HasColumnType("numeric(6,3)");

        builder.Property(s => s.PlayTime);
        builder.Property(s => s.StartTime);

        builder.HasOne(s => s.Beatmap)
            .WithMany(b => b.Scores)
            .HasForeignKey(s => s.MapId)
            .HasPrincipalKey(b => b.MapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Player)
            .WithMany(p => p.Scores)
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Ignore(s => s.Passed);
    }
}