using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Notifications;

public class Cursor
{
    public int Id { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }
}