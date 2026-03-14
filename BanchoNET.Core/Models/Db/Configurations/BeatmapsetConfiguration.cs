using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class BeatmapsetConfiguration : IEntityTypeConfiguration<BeatmapsetDto>
{
    public void Configure(
        EntityTypeBuilder<BeatmapsetDto> builder
    ) {
        builder.ToTable("Beatmapsets");

        builder.HasKey(bs => bs.SetId);

        builder
            .HasOne(bs => bs.Owner)
            .WithMany(p => p.Beatmapsets)
            .HasForeignKey(bs => bs.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}