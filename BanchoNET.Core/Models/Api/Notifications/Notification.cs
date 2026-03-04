namespace BanchoNET.Core.Models.Api.Notifications;

public class Notification
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required string ObjectType { get; set; }
    public int ObjectId { get; set; }
    public int SourceUserId { get; set; }
    public bool IsRead { get; set; }
    public Details Details { get; set; } = new();
}