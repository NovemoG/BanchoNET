using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Json;
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
        builder.HasIndex(s => s.ModKeys);
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
        
        builder.Property(b => b.ModKeys)
            .HasMaxLength(128)
            .IsRequired()
            .IsUnicode();

        builder.Property(b => b.LazerMods)
            .HasMaxLength(2048)
            .IsRequired(false)
            .IsUnicode();
        
        builder.Property(s => s.PP)
            .HasColumnType("numeric(7,3)");

        builder.Property(s => s.Acc)
            .HasColumnType("numeric(6,3)");

        builder.Property(s => s.PlayTime);
        builder.Property(s => s.StartTime);

        builder.Property(x => x.Statistics)
            .HasConversion(DatabaseDictionaryConverter.StatisticsConverter);

        builder.HasOne(s => s.Beatmap)
            .WithMany(b => b.Scores)
            .HasForeignKey(s => s.MapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Player)
            .WithMany(p => p.Scores)
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.Passed);
    }
}