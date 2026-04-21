using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Notify;

public class ChatMessage
{
    [JsonInclude, JsonPropertyName("message_id")]
    public readonly long? Id;
    public long ChannelId { get; set; }
    public bool IsAction { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Content { get; set; }
    public BasicApiPlayer Sender { get; set; }
    public int SenderId
    {
        get => Sender.Id;
        set => Sender.Id = value;
    }
    public string Uuid { get; set; } = string.Empty;
    
    [JsonConstructor]
    public ChatMessage() { }
    
    public ChatMessage(
        MessageDto message
    ) {
        Id = message.Id;
    }
}