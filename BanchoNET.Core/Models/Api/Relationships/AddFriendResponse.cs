namespace BanchoNET.Core.Models.Api.Relationships;

public class AddFriendResponse
{
    public int TargetId { get; set; }
    public string RelationType { get; set; } = "friend";
    public bool Mutual { get; set; }
}