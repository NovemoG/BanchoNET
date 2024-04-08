using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[PrimaryKey(nameof(Id))]
public class ClientHashesDto
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	[Column(TypeName = "CHAR"), StringLength(32), Unicode(false)]
	public string OsuPath { get; set; }

	[Column(TypeName = "CHAR"), StringLength(32), Unicode(false)]
	public string Adapters { get; set; }

	[Column(TypeName = "CHAR"), StringLength(32), Unicode(false)]
	public string Uninstall { get; set; }

	[Column(TypeName = "CHAR"), StringLength(32), Unicode(false)]
	public string DiskSerial { get; set; }
	
	[Column(TypeName = "DATETIME")]
	public DateTime LatestTime { get; set; }
	
	[ForeignKey("PlayerId")]
	public PlayerDto Player { get; set; } = null!;
	public int PlayerId { get; set; }
}