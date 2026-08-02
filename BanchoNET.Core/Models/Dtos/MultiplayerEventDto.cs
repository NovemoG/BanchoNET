using BanchoNET.Core.Models.Multiplayer;

namespace BanchoNET.Core.Models.Dtos;

public class MultiplayerEventDto
{
	public long Id { get; set; }
	public long MatchId { get; set; }

	public MultiplayerEventType Type { get; set; }

	/// <summary>
	/// Null for match-level events, and cleared when the player is deleted.
	/// </summary>
	public int? UserId { get; set; }

	/// <summary>
	/// Set on <see cref="MultiplayerEventType.GamePlayed"/> only.
	/// </summary>
	public long? GameId { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public MultiplayerMatchDto Match { get; set; } = null!;
	public MultiplayerGameDto? Game { get; set; }
}
