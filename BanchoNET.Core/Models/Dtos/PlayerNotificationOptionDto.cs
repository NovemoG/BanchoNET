namespace BanchoNET.Core.Models.Dtos;

public class PlayerNotificationOptionDto
{
	public int Id { get; set; }
	public int PlayerId { get; set; }

	public string Name { get; set; } = null!;
	public string Details { get; set; } = "{}";

	public DateTime UpdatedAt { get; set; }

	public PlayerDto Player { get; set; } = null!;
}