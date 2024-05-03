using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[PrimaryKey(nameof(Id))]
public class ChannelDto
{
	[Key] public int Id { get; set; }
	
	public string Name { get; set; }
	public string Description { get; set; }
	public bool AutoJoin { get; set; }
	public bool Hidden { get; set; }
	public bool ReadOnly { get; set; }
	public int ReadPrivileges { get; set; }
	public int WritePrivileges { get; set; }
}