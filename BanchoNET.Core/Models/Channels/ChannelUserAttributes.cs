namespace BanchoNET.Core.Models.Channels;

public class ChannelUserAttributes
{
    public bool CanListUsers { get; set; }
    public bool CanMessage { get; set; } = true;
    public bool? CanMessageError { get; set; }
    public long? LastReadId { get; set; }
}