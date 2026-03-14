using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<PlayerDto>
{
    public void Configure(
        EntityTypeBuilder<PlayerDto> builder
    ) {
        builder.ToTable("Players");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.Username).IsUnique();
        builder.HasIndex(p => p.SafeName).IsUnique();
        builder.HasIndex(p => p.LoginName).IsUnique();
        builder.HasIndex(p => p.Email).IsUnique();
        builder.HasIndex(p => p.ApiKey).IsUnique();
        builder.HasIndex(p => p.Country);
        builder.HasIndex(p => p.Privileges);

        builder.Property(p => p.Username)
            .IsRequired()
            .HasMaxLength(16)
            .IsUnicode(false);
        
        builder.Property(p => p.SafeName)
            .IsRequired()
            .HasMaxLength(16)
            .IsUnicode(false);
        
        builder.Property(p => p.LoginName)
            .IsRequired()
            .HasMaxLength(16)
            .IsUnicode(false);
        
        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(160)
            .IsUnicode(false);
        
        builder.Property(p => p.PasswordHash)
            .IsRequired()
            .HasColumnType("CHAR(60)")
            .IsUnicode(false);
        
        builder.Property(p => p.Country)
            .IsRequired()
            .HasColumnType("CHAR(2)")
            .IsUnicode(false);
        
        builder.Property(p => p.ApiKey)
            .HasColumnType("CHAR")
            .HasMaxLength(36)
            .IsUnicode(false);

        builder.Property(p => p.RemainingSilence)
            .HasColumnType("DATETIME");
        builder.Property(p => p.RemainingSupporter)
            .HasColumnType("DATETIME");
        builder.Property(p => p.CreationTime)
            .HasColumnType("DATETIME");
        builder.Property(p => p.LastActivityTime)
            .HasColumnType("DATETIME");

        builder.Property(p => p.SupporterLevel)
            .HasColumnType("TINYINT(2)");
        builder.Property(p => p.PreferredMode)
            .HasColumnType("TINYINT(2)");

        builder.Property(p => p.AwayMessage)
            .HasMaxLength(128);
        builder.Property(p => p.UserPageContent)
            .HasMaxLength(4096);

        builder.Ignore(p => p.IsSupporter);
    }
}