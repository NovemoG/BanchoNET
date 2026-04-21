using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Channels;

public class ChannelMessage
{
    public long ChannelId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsAction { get; set; }
    public long MessageId { get; set; }
    public int SenderId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Type { get; set; } = "plain";
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Uuid { get; set; }
    
    public BasicApiPlayer Sender { get; set; } = null!;
    
    [JsonConstructor]
    public ChannelMessage() { }

    public ChannelMessage(
        MessageDto message,
        string? uuid = null
    ) {
        ChannelId = message.ChannelId;
        Content = message.Message;
        IsAction = message.IsAction;
        MessageId = message.Id;
        SenderId = message.SenderId;
        Timestamp = message.SentAt;
        Type = IsAction ? "action" : "plain";
        Sender = new BasicApiPlayer(message.Sender);
        Uuid = uuid;
    }
}