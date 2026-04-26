using System.ComponentModel.DataAnnotations;

namespace BanchoNET.Core.Models.Dtos;

public class MessageDto
{
    public long Id { get; set; }
    public bool IsAction { get; set; }
    public DateTime SentAt { get; set; }
    
    [MaxLength(2048)]
    public string Message { get; set; } = null!;
    
    public int SenderId { get; set; }
    public PlayerDto Sender { get; set; } = null!;
    
    public long ChannelId { get; set; }
    public ChannelDto Channel { get; set; } = null!;
}