using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class SkillsConfiguration : IEntityTypeConfiguration<SkillsDto>
{
    public void Configure(
        EntityTypeBuilder<SkillsDto> builder
    ) {
        builder.ToTable("Skills");
        
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PlayerId).IsUnique();
        builder.HasIndex(x => x.BeatmapId).IsUnique();

        builder.Property(x => x.Skills)
            .HasConversion(DatabaseDictionaryConverter.SkillsConverter);

        builder.HasOne(x => x.Player)
            .WithOne(x => x.Skills)
            .HasForeignKey<SkillsDto>(x => x.PlayerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Beatmap)
            .WithOne(x => x.Skills)
            .HasForeignKey<SkillsDto>(x => x.BeatmapId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_Skills_ExactlyOneOwner",
                "(\"PlayerId\" IS NOT NULL AND \"BeatmapId\" IS NULL) OR (\"PlayerId\" IS NULL AND \"BeatmapId\" IS NOT NULL)"
            ));
    }
}