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
        
        builder.Property(p => p.Title)
            .HasMaxLength(64)
            .IsUnicode();
        
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

        builder.Property(p => p.RemainingSilence);
        builder.Property(p => p.RemainingSupporter);
        builder.Property(p => p.CreationTime);
        builder.Property(p => p.LastActivityTime);

        builder.Property(p => p.SupporterLevel);
        builder.Property(p => p.PreferredMode);

        builder.Property(p => p.AwayMessage)
            .HasMaxLength(128);
        builder.Property(p => p.UserPageContent)
            .HasMaxLength(4096);

        builder.Property(p => p.PlayStyle)
            .HasConversion<byte>();

        builder.Property(p => p.UserFrom).HasMaxLength(100);
        builder.Property(p => p.UserInterests).HasMaxLength(255);
        builder.Property(p => p.UserOcc).HasMaxLength(255);
        builder.Property(p => p.UserTwitter).HasMaxLength(255);
        builder.Property(p => p.UserDiscord).HasMaxLength(37);
        builder.Property(p => p.UserWebsite).HasMaxLength(200);
        builder.Property(p => p.UserSig).HasMaxLength(3000);
        builder.Property(p => p.UserNotify).HasDefaultValue(true);

        builder.Property(p => p.AvatarExtension)
            .HasMaxLength(8)
            .IsUnicode(false);
        builder.Property(p => p.CoverFile)
            .HasMaxLength(64)
            .IsUnicode(false);

        builder.Ignore(p => p.IsSupporter);
    }
}