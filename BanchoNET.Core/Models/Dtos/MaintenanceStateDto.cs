using System.ComponentModel.DataAnnotations;

namespace BanchoNET.Core.Models.Dtos;

public class MaintenanceStateDto
{
	[Key]
	[MaxLength(64)]
	public string Key { get; set; } = null!;

	public DateTime LastRunAt { get; set; }

	[MaxLength(512)]
	public string? Details { get; set; }
}