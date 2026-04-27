namespace BanchoNET.Core.Models.Api;

public class AddFriendResponse
{
    public int TargetId { get; set; }
    public string RelationType { get; set; } = "friend";
    public bool Mutual { get; set; }
}