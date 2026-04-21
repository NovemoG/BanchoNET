using System.ComponentModel.DataAnnotations;
using BanchoNET.Core.Models.Channels;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Models.Dtos;

[PrimaryKey(nameof(Id))]
public class ChannelDto
{
	[Key] public long Id { get; set; }
	
	[MaxLength(16)]
	public required string Name { get; set; }
	[MaxLength(128)]
	public required string Description { get; set; }
	
	public bool AutoJoin { get; set; }
	public bool Hidden { get; set; }
	public bool ReadOnly { get; set; }
	public ChannelType Type { get; set; }
	public int ReadPrivileges { get; set; }
	public int WritePrivileges { get; set; }
	
	public long? LastMessageId { get; set; }

	// will be empty if it is not a PM type channel
	public ICollection<PlayerDto> Players { get; set; } = new List<PlayerDto>();
	public ICollection<MessageDto> Messages { get; set; } = new List<MessageDto>();
}