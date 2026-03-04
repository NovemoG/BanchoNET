namespace BanchoNET.Core.Models.Api.Relationships;

public class Relationship
{
    public int TargetId { get; set; }
    public required string RelationType { get; set; }
    public bool Mutual { get; set; }
    public required TargetPlayer Target { get; set; }
}