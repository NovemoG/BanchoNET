namespace BanchoNET.Core.Models.Api.Chat;

public class ChatPostRequest
{
    public string type { get; set; } = null!;
    public long target_id { get; set; }
}