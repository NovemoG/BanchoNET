using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Channels;

namespace BanchoNET.Core.Models.Notify;

public class NewChatMessageData
{
    public List<ChannelMessage> Messages { get; set; } = null!;
    public List<BasicApiPlayer> Users { get; set; } = null!;
}