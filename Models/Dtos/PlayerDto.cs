using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[Index(nameof(Username), IsUnique = true)]
[Index(nameof(SafeName), IsUnique = true)]
[Index(nameof(LoginName), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
[Index(nameof(ApiKey), IsUnique = true)]
[PrimaryKey("Id")]
public class PlayerDto
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	[MaxLength(16), Unicode(false)]
	public string Username { get; set; }
	
	[Key, MaxLength(16), Unicode(false)]
	public string SafeName { get; set; }
	
	[Key, MaxLength(16), Unicode(false)]
	public string LoginName { get; set; }
	
	[MaxLength(160), Unicode(false)]
	public string Email { get; set; }
	
	[Column(TypeName = "CHAR(60)"), Unicode(false)]
	public string PasswordHash { get; set; }
	
	[Column(TypeName = "CHAR(2)"), Unicode(false)]
	public string Country { get; set; }
	
	public int Privileges { get; set; }
	public int RemainingSilence { get; set; }
	public int RemainingSupporter { get; set; }
	public DateTime CreationTime { get; set; }
	public DateTime LastActivityTime { get; set; }
	public byte PreferredMode { get; set; }
	public byte PlayStyle { get; set; }
	
	[MaxLength(4096)] 
	public string? UserPageContent { get; set; }
	
	[Column(TypeName = "CHAR"), StringLength(36), Unicode(false)]
	public string? ApiKey { get; set; }
}