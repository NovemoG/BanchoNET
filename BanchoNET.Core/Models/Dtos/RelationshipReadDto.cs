namespace BanchoNET.Core.Models.Dtos;

public class RelationshipReadDto
{
    public int PlayerId { get; set; }
    public int TargetId { get; set; }
    public byte Relation { get; set; }
    public bool Mutual { get; set; }
    public PlayerDto Target { get; set; } = null!;
}