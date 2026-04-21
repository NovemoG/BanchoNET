using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<MessageDto>
{
    public void Configure(
        EntityTypeBuilder<MessageDto> builder
    ) {
        builder.ToTable("Messages");
        
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.ReceiverId);
        builder.HasIndex(m => m.ChannelId);
        builder.HasIndex(m => m.Read);
        builder.HasIndex(m => m.SentAt);

        builder.Property(m => m.Message)
            .IsRequired()
            .HasMaxLength(2048);
        
        builder.HasOne(m => m.Sender)
            .WithMany(p => p.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(m => m.Receiver)
            .WithMany(p => p.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(m => m.Channel)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChannelId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}