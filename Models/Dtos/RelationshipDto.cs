using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[PrimaryKey(nameof(Id))]
public class RelationshipDto
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public uint Id { get; set; }
	
	public int PlayerId { get; set; }
	public int TargetId { get; set; }
	
	public byte Relation { get; set; }
	
	[ForeignKey("PlayerId")]
	public PlayerDto Player { get; set; } = null!;
}