using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Channels;

namespace BanchoNET.Core.Models.Notify;

public class SocketChannel
{
    [JsonPropertyName("users")]
    public List<int> UserIds { get; set; } = [];
    
    [JsonPropertyName("channel_id")]
    public long Id { get; set; }
    
    public string Name { get; set; }
    public string Topic { get; set; } = string.Empty;
    public ChannelType Type { get; set; }
    public long? LastMessageId { get; set; }
    public long? LastReadId { get; set; }
    public int? MessageLengthLimit { get; set; }
}