using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class BeatmapConfiguration : IEntityTypeConfiguration<BeatmapDto>
{
    public void Configure(
        EntityTypeBuilder<BeatmapDto> builder
    ) {
        builder.ToTable("Beatmaps");

        builder.HasKey(b => b.MapId);
        
        builder.HasIndex(b => b.MapId);
        builder.HasIndex(b => b.SetId);
        builder.HasIndex(b => b.MD5).IsUnique();

        builder.Property(b => b.MD5)
            .IsRequired()
            .HasColumnType("CHAR(32)")
            .IsUnicode();

        builder.Property(b => b.Artist)
            .HasMaxLength(128)
            .IsUnicode(false);

        builder.Property(b => b.ArtistUnicode)
            .HasDefaultValue(string.Empty)
            .HasMaxLength(128);

        builder.Property(b => b.Title)
            .HasMaxLength(128)
            .IsUnicode(false);

        builder.Property(b => b.TitleUnicode)
            .HasDefaultValue(string.Empty)
            .HasMaxLength(128);

        builder.Property(b => b.Name)
            .HasMaxLength(128)
            .IsUnicode(false);

        builder.Property(b => b.CreatorName)
            .HasMaxLength(16)
            .IsUnicode(false);

        builder.Property(b => b.Tags)
            .HasDefaultValue(string.Empty)
            .HasMaxLength(1024)
            .IsUnicode(false);

        builder.Property(b => b.SubmitDate)
            .HasColumnType("DATETIME");
        builder.Property(b => b.LastUpdate)
            .HasColumnType("DATETIME");
        builder.Property(b => b.RankedDate)
            .HasColumnType("DATETIME");

        builder.Property(b => b.Bpm).HasColumnType("FLOAT(15,3)");
        builder.Property(b => b.Cs).HasColumnType("FLOAT(4,2)");
        builder.Property(b => b.Ar).HasColumnType("FLOAT(4,2)");
        builder.Property(b => b.Od).HasColumnType("FLOAT(4,2)");
        builder.Property(b => b.Hp).HasColumnType("FLOAT(4,2)");
        builder.Property(b => b.StarRating).HasColumnType("FLOAT(9,3)");

        builder.Ignore(b => b.SliderTailHit);
        builder.Ignore(b => b.NotesCount);
        
        builder
            .HasOne(b => b.Creator)
            .WithMany(b => b.Beatmaps)
            .HasForeignKey(b => b.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(b => b.Beatmapset)
            .WithMany(bs => bs.Beatmaps)
            .HasForeignKey(b => b.SetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}