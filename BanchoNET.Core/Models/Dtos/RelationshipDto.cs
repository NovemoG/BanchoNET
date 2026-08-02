namespace BanchoNET.Core.Models.Dtos;

public class RelationshipDto
{
	public uint Id { get; set; }
	
	public int PlayerId { get; set; }
	public PlayerDto Player { get; set; } = null!;
	
	public int TargetId { get; set; }
	public PlayerDto Target { get; set; } = null!;
	
	public byte Relation { get; set; }
}