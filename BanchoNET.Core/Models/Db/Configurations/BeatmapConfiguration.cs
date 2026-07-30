using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class BeatmapConfiguration : IEntityTypeConfiguration<BeatmapDto>
{
    public void Configure(
        EntityTypeBuilder<BeatmapDto> builder
    ) {
        builder.ToTable("Beatmaps");

        builder.HasKey(b => b.Id);
        
        builder.HasIndex(b => b.SetId);
        builder.HasIndex(b => b.MD5).IsUnique();
        builder.HasIndex(b => b.Mode);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.Plays);
        builder.HasIndex(b => b.OwnerId);

        builder.Property(b => b.OwnerName)
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.Property(b => b.MD5)
            .IsRequired()
            .HasColumnType("CHAR(32)")
            .IsUnicode(false);

        builder.Property(b => b.Version)
            .HasMaxLength(128)
            .IsUnicode(false);

        builder.Property(b => b.LastUpdated);

        builder.Property(b => b.Bpm).HasColumnType("numeric(15,3)");
        builder.Property(b => b.Cs).HasColumnType("numeric(4,2)");
        builder.Property(b => b.Ar).HasColumnType("numeric(4,2)");
        builder.Property(b => b.Od).HasColumnType("numeric(4,2)");
        builder.Property(b => b.Hp).HasColumnType("numeric(4,2)");
        builder.Property(b => b.StarRating).HasColumnType("numeric(9,3)");

        builder.Property(x => x.MaximumStatistics)
            .HasConversion(DatabaseDictionaryConverter.StatisticsConverter);

        builder
            .HasOne(b => b.Beatmapset)
            .WithMany(bs => bs.Beatmaps)
            .HasForeignKey(b => b.SetId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder
            .HasMany(b => b.Scores)
            .WithOne(s => s.Beatmap)
            .HasForeignKey(s => s.MapId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}