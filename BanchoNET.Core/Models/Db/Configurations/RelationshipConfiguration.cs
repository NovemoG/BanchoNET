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

        builder.HasIndex(r => new { r.PlayerId, r.TargetId, r.Relation }).IsUnique();

        builder.Property(r => r.Relation).IsRequired();

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