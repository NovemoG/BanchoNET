namespace BanchoNET.Core.Models.Api.Chat;

public class ChatNewRequest
{
    public int target_id { get; set; }
    public string message { get; set; }
    public bool is_action { get; set; }
    public string uuid { get; set; }
}