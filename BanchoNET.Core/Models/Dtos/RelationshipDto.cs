using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Models.Dtos;

[Index(nameof(PlayerId))]
[Index(nameof(TargetId))]
[Index(nameof(Relation))]
[PrimaryKey(nameof(Id))]
public class RelationshipDto
{
	[Key] public uint Id { get; set; }
	
	public int PlayerId { get; set; }
	public int TargetId { get; set; }
	
	public byte Relation { get; set; }
	public bool IsMutual { get; set; } //TODO
	
	[ForeignKey(nameof(PlayerId))]
	public PlayerDto Player { get; set; } = null!;
	
	[ForeignKey(nameof(TargetId))]
	public PlayerDto Target { get; set; } = null!;
}