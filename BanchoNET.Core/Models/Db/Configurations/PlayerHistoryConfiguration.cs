using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class PlayerHistoryConfiguration : IEntityTypeConfiguration<PlayerHistoryDto>
{
    public void Configure(
        EntityTypeBuilder<PlayerHistoryDto> builder
    ) {
        builder.ToTable("PlayerHistories");
        
        builder.HasKey(h => new { h.PlayerId, h.Mode, h.Metric, h.Granularity, h.Date });
        
        builder.HasIndex(h => h.Date)
            .HasMethod("brin");

        builder.Property(h => h.Metric)
            .HasConversion<short>();

        builder.Property(h => h.Granularity)
            .HasConversion<short>();

        builder.Property(h => h.Date)
            .HasColumnType("date");
        
        builder.HasOne<PlayerDto>()
            .WithMany()
            .HasForeignKey(h => h.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}