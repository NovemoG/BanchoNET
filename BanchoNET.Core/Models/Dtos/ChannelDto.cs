using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Models.Dtos;

[PrimaryKey(nameof(Id))]
public class ChannelDto
{
	[Key] public int Id { get; set; }
	
	[MaxLength(16)]
	public required string Name { get; set; }
	[MaxLength(128)]
	public required string Description { get; set; }
	
	public bool AutoJoin { get; set; }
	public bool Hidden { get; set; }
	public bool ReadOnly { get; set; }
	public int ReadPrivileges { get; set; }
	public int WritePrivileges { get; set; }
}