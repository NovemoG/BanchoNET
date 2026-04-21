using System.Text.Json;

namespace BanchoNET.Core.Models.Notify;

public class SocketMessage
{
    public string Event { get; set; } = null!;
    public JsonElement? Data { get; set; }
    public string? Error { get; set; }
}