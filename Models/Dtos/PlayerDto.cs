﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[Index(nameof(Username), IsUnique = true)]
[Index(nameof(SafeName), IsUnique = true)]
[Index(nameof(LoginName), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
[Index(nameof(ApiKey), IsUnique = true)]
[Index(nameof(Country))]
[Index(nameof(Privileges))]
[PrimaryKey(nameof(Id))]
public class PlayerDto
{
	[Key] public int Id { get; set; }
	
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
	
	[Column(TypeName = "DATETIME")]
	public DateTime RemainingSilence { get; set; }
	[Column(TypeName = "DATETIME")]
	public DateTime RemainingSupporter { get; set; }
	
	[Column(TypeName = "DATETIME")]
	public DateTime CreationTime { get; set; }
	[Column(TypeName = "DATETIME")]
	public DateTime LastActivityTime { get; set; }
	
	[Column(TypeName = "TINYINT(2)")]
	public byte PreferredMode { get; set; }
	public byte PlayStyle { get; set; }
	
	[MaxLength(128)]
	public string? AwayMessage { get; set; }

	[MaxLength(4096)] 
	public string? UserPageContent { get; set; }

	[Column(TypeName = "CHAR"), StringLength(36), Unicode(false)]
	public string? ApiKey { get; set; }

	public ICollection<StatsDto> Stats { get; set; } = null!;
	public ICollection<ScoreDto> Scores { get; set; } = null!;
	public ICollection<LoginDto> LoginsData { get; set; } = null!;
	public ICollection<ClientHashesDto> ClientHashes { get; set; } = null!;
	public ICollection<RelationshipDto> Relationships { get; set; } = null!;
}