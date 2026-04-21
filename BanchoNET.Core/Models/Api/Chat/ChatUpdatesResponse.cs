using BanchoNET.Core.Models.Channels;

namespace BanchoNET.Core.Models.Api.Chat;

public class ChatUpdatesResponse
{
    public List<ChatChannelExtended> Presence { get; set; } = [];
}