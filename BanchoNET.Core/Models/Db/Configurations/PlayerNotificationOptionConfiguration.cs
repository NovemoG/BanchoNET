using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class PlayerNotificationOptionConfiguration : IEntityTypeConfiguration<PlayerNotificationOptionDto>
{
    public void Configure(
        EntityTypeBuilder<PlayerNotificationOptionDto> builder
    ) {
        builder.ToTable("PlayerNotificationOptions");

        builder.HasKey(o => o.Id);

        builder.HasIndex(o => new { o.PlayerId, o.Name }).IsUnique();

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(64)
            .IsUnicode(false);

        builder.Property(o => o.Details)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        builder.HasOne(o => o.Player)
            .WithMany(p => p.NotificationOptions)
            .HasForeignKey(o => o.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}