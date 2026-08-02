namespace BanchoNET.Core.Models.Dtos;

public class MultiplayerParticipantDto
{
	public long MatchId { get; set; }
	public int PlayerId { get; set; }

	public MultiplayerMatchDto Match { get; set; } = null!;
	public PlayerDto Player { get; set; } = null!;
}