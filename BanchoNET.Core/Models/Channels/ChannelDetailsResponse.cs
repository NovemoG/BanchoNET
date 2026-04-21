using BanchoNET.Core.Models.Api.Player;

namespace BanchoNET.Core.Models.Channels;

public class ChannelDetailsResponse
{
    public required ChatChannelExtended Channel { get; set; }
    public List<BasicApiPlayer> Users { get; set; } = [];
}