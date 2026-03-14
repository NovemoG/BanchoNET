using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class RelationshipConfiguration : IEntityTypeConfiguration<RelationshipDto>
{
    public void Configure(
        EntityTypeBuilder<RelationshipDto> builder
    ) {
        builder.ToTable("Relationships");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.PlayerId);
        builder.HasIndex(r => r.TargetId);
        builder.HasIndex(r => r.Relation);

        builder.Property(r => r.Relation).IsRequired();
        builder.Property(r => r.IsMutual).IsRequired();

        builder
            .HasOne(r => r.Player)
            .WithMany(r => r.Relationships)
            .HasForeignKey(r => r.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder
            .HasOne(r => r.Target)
            .WithMany(r => r.IncomingRelationships)
            .HasForeignKey(r => r.TargetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}