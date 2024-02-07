using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[PrimaryKey(nameof(PlayerId))]
public class RelationshipDto
{
	[Key, ForeignKey("Player"), DatabaseGenerated(DatabaseGeneratedOption.None)] 
	public int PlayerId { get; set; }
	public int TargetId { get; set; }
	public byte Relation { get; set; }
}