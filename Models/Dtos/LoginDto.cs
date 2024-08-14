using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[PrimaryKey(nameof(Id))]
public class LoginDto
{
	[Key] public int Id { get; set; }
	
	[MaxLength(45), Unicode(false)]
	public required string Ip { get; set; }
	[MaxLength(11), Unicode(false)]
	public required string ReleaseStream { get; set; }
	
	[Column(TypeName = "DATETIME")]
	public DateTime OsuVersion { get; set; }
	
	[Column(TypeName = "DATETIME")]
	public DateTime LoginTime { get; set; }
	
	[ForeignKey("PlayerId")]
	public PlayerDto Player { get; set; } = null!;
	public int PlayerId { get; set; }
}