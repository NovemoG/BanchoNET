using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils;

namespace BanchoNET.Core.Models.Channels;

public class ChatChannel
{
    [JsonPropertyName("channel_id")]
    public long? Id { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int? MessageLengthLimit { get; set; } = 450;
    public bool Moderated { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Uuid { get; set; }
    
    [JsonConstructor]
    public ChatChannel() { }

    public ChatChannel(
        Channel channel
    ) {
        Id = channel.Id;
        Description = channel.Description;
        MessageLengthLimit = 450;
        Name = channel.Name;
        Type = channel.Type.ToString().ToUpper();
    }

    public ChatChannel(
        ChannelDto channel,
        int playerId
    ) {
        var target = channel.ChannelPlayers.Single(p => p.PlayerId != playerId).Player;
        
        Id = channel.Id;
        Description = "";
        MessageLengthLimit = 450;
        Name = target.Username;
        Type = channel.Type.ToString().ToUpper();
        Icon = $"https://a.{AppSettings.Domain}/{target.Id}";
    }
}