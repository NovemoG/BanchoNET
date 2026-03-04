namespace BanchoNET.Core.Models.Api.Notifications;

public class Stack
{
    public required string Category { get; set; }
    public Cursor? Cursor { get; set; }
    public required string Name { get; set; }
    public required string ObjectType { get; set; }
    public int ObjectId { get; set; }
    public int Total { get; set; }
}