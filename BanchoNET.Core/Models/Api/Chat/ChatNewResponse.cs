using BanchoNET.Core.Models.Channels;

namespace BanchoNET.Core.Models.Api.Chat;

public class ChatNewResponse
{
    public required ChatChannelExtended Channel { get; set; }
    public required ChannelMessage Message { get; set; }
    public long NewChannelId { get; set; }
}