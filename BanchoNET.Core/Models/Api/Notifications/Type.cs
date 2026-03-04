namespace BanchoNET.Core.Models.Api.Notifications;

public class Type
{
    public Cursor Cursor { get; set; } = new();
    public string? Name { get; set; }
    public int Total { get; set; }
}