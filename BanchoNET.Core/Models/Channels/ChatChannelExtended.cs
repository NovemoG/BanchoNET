using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Channels;

public class ChatChannelExtended : ChatChannel
{
    public ChannelUserAttributes CurrentUserAttributes { get; set; } = new();
    public long? LastMessageId { get; set; }
    public long? LastReadId { get; set; }
    
    [JsonPropertyName("users")]
    public List<int> UserIds { get; set; } = [];
    
    [JsonConstructor]
    public ChatChannelExtended() { }

    public ChatChannelExtended(
        Channel channel
    ) : base(channel) {
        if (channel.Type is ChannelType.Announce)
            CurrentUserAttributes.CanMessage = false;

        CurrentUserAttributes.LastReadId = channel.LastMessageId;
        LastMessageId = channel.LastMessageId;
        LastReadId = channel.LastMessageId;
    }
    
    public ChatChannelExtended(
        ChannelDto channel,
        int playerId
    ) : base(channel, playerId) {
        if (channel.Type is ChannelType.Announce)
            CurrentUserAttributes.CanMessage = false;

        CurrentUserAttributes.LastReadId = channel.LastMessageId;
        LastMessageId = channel.LastMessageId;
        LastReadId = channel.LastMessageId;

        UserIds.AddRange(channel.Players.Select(p => p.Id));
    }
}