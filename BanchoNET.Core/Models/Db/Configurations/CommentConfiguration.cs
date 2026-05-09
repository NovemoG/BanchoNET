using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<CommentDto>
{
    public void Configure(
        EntityTypeBuilder<CommentDto> builder
    ) {
        builder.ToTable("Comments");

        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.ThreadId);
        builder.HasIndex(x => x.ReplyToId);
        builder.HasIndex(x => x.AuthorId);
        builder.HasIndex(x => x.EditedById);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.VotesCount);

        builder.Property(x => x.Message)
            .HasMaxLength(1024);

        builder.Property(x => x.MessageHtml)
            .HasMaxLength(16384);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.DeletedAt);
        builder.Property(x => x.EditedAt);
        
        builder.Property(x => x.VotesCount)
            .IsRequired();
        
        builder.Property(x => x.RepliesCount)
            .IsRequired();
        
        builder.Property(x => x.Pinned)
            .IsRequired();

        builder.Property(x => x.AuthorId)
            .IsRequired();

        builder.Property(x => x.ThreadId)
            .IsRequired();

        builder.Property(x => x.EditedById);

        builder.HasOne(x => x.Author)
            .WithMany(b => b.Comments)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Editor)
            .WithMany()
            .HasForeignKey(x => x.EditedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Thread)
            .WithMany(b => b.Comments)
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ReplyTo)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ReplyToId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}