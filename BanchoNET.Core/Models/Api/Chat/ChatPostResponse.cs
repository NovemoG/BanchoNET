using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Api.Chat;

public class ChatPostResponse : ChatChannel
{
    public List<ChannelMessage> Messages { get; set; } = [];

    [JsonConstructor]
    public ChatPostResponse() { }
    
    public ChatPostResponse(
        ChannelDto channel,
        int playerId,
        List<MessageDto> messages
    ) : base(channel, playerId) {
        Messages = messages.Select(m => new ChannelMessage(m)).ToList();
    }
}