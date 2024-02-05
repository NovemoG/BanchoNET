using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[PrimaryKey("UserId", "TargetId")]
public class RelationshipDto
{
	[Key, ForeignKey("Player")] public int UserId { get; set; }
	[Key, ForeignKey("Player")] public int TargetId { get; set; }
	public byte Type { get; set; }
}