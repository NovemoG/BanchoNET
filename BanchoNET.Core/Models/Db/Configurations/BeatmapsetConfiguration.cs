using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class BeatmapsetConfiguration : IEntityTypeConfiguration<BeatmapsetDto>
{
    private const string RatingSql =
        """
        COALESCE((
            "Ratings"[1] * 1 + "Ratings"[2] * 2 + "Ratings"[3] * 3 + "Ratings"[4] * 4 +
            "Ratings"[5] * 5 + "Ratings"[6] * 6 + "Ratings"[7] * 7 + "Ratings"[8] * 8 +
            "Ratings"[9] * 9 + "Ratings"[10] * 10
        )::real / NULLIF(
            "Ratings"[1] + "Ratings"[2] + "Ratings"[3] + "Ratings"[4] + "Ratings"[5] +
            "Ratings"[6] + "Ratings"[7] + "Ratings"[8] + "Ratings"[9] + "Ratings"[10]
        , 0), 0)
        """;
    
    public void Configure(
        EntityTypeBuilder<BeatmapsetDto> builder
    ) {
        builder.ToTable("Beatmapsets");

        builder.HasKey(bs => bs.Id);

        builder.Property(b => b.Rating)
            .HasComputedColumnSql(RatingSql, stored: true);
        
        builder.HasIndex(b => b.Id);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.PlayCount);
        builder.HasIndex(b => b.SubmittedDate);
        builder.HasIndex(b => b.LastUpdated);
        builder.HasIndex(b => b.RankedDate);

        builder.Property(b => b.CreatorName)
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.Property(b => b.Tags)
            .HasDefaultValue(string.Empty)
            .HasMaxLength(2048)
            .IsUnicode();

        builder.Property(b => b.Description)
            .HasDefaultValue(string.Empty)
            .HasMaxLength(131072)
            .IsUnicode();

        builder.Property(b => b.Artist)
            .HasMaxLength(128)
            .IsUnicode(false);

        builder.Property(b => b.ArtistUnicode)
            .HasMaxLength(128)
            .IsUnicode();

        builder.Property(b => b.Title)
            .HasMaxLength(128)
            .IsUnicode(false);

        builder.Property(b => b.TitleUnicode)
            .HasMaxLength(128)
            .IsUnicode();

        builder.Property(b => b.Source)
            .HasMaxLength(128)
            .IsUnicode();

        builder.Property(b => b.SubmittedDate);
        builder.Property(b => b.LastUpdated);
        builder.Property(b => b.RankedDate)
            .IsRequired(false);

        builder
            .HasOne(bs => bs.Creator)
            .WithMany(p => p.Beatmapsets)
            .HasForeignKey(bs => bs.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder
            .HasOne(t => t.Thread)
            .WithOne(bs => bs.Beatmapset)
            .HasForeignKey<ThreadDto>(t => t.BeatmapsetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}