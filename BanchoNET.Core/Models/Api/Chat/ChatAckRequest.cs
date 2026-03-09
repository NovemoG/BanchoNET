namespace BanchoNET.Core.Models.Api.Chat;

public class ChatAckRequest
{
    public long since { get; set; }
    public long history_since { get; set; }
}