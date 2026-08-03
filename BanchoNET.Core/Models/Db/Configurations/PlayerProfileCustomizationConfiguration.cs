using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class PlayerProfileCustomizationConfiguration : IEntityTypeConfiguration<PlayerProfileCustomizationDto>
{
    public void Configure(
        EntityTypeBuilder<PlayerProfileCustomizationDto> builder
    ) {
        builder.ToTable("PlayerProfileCustomizations");

        builder.HasKey(c => c.PlayerId);

        builder.Property(c => c.PlayerId).ValueGeneratedNever();

        builder.Property(c => c.BeatmapsetDownload)
            .HasConversion<string>()
            .HasMaxLength(8)
            .IsUnicode(false)
            .HasDefaultValue(BeatmapsetDownloadType.All);

        builder.Property(c => c.BeatmapsetShowNsfw).HasDefaultValue(false);
        builder.Property(c => c.BeatmapsetShowAnimeCover).HasDefaultValue(true);
        builder.Property(c => c.BeatmapsetTitleShowOriginal).HasDefaultValue(false);

        builder.Property(c => c.ExtrasOrder).HasColumnType("jsonb");

        builder.HasOne(c => c.Player)
            .WithOne(p => p.ProfileCustomization)
            .HasForeignKey<PlayerProfileCustomizationDto>(c => c.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}