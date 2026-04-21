namespace BanchoNET.Core.Models.Api.Chat;

public class ChatMessageRequest
{
    public bool is_action { get; set; }
    public required string message { get; set; }
    public required string uuid { get; set; }
}