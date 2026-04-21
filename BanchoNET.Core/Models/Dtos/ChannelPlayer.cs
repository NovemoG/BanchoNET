namespace BanchoNET.Core.Models.Dtos;

public class ChannelPlayer
{
    public int PlayerId { get; set; }
    public PlayerDto Player { get; set; } = null!;
    
    public long ChannelId { get; set; }
    public ChannelDto Channel { get; set; } = null!;
    
    public long? LastReadMessageId { get; set; }
}