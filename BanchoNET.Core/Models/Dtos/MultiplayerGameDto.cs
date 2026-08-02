using BanchoNET.Core.Models.Stable.Multiplayer;

namespace BanchoNET.Core.Models.Dtos;

public class MultiplayerGameDto
{
	public long Id { get; set; }
	public long MatchId { get; set; }

	public int? BeatmapId { get; set; }
	public required string BeatmapMD5 { get; set; }
	public required string BeatmapName { get; set; }

	public byte Mode { get; set; }
	public WinCondition WinCondition { get; set; }
	public LobbyType LobbyType { get; set; }

	/// <summary>
	/// Lobby-wide mods, 0 when freemods is on and the mods live on each score instead.
	/// </summary>
	public int Mods { get; set; }

	public DateTimeOffset StartTime { get; set; }
	public DateTimeOffset? EndTime { get; set; }

	public bool Aborted { get; set; }

	/// <summary>
	/// The game was closed by the completion timeout rather than by every client reporting in.
	/// </summary>
	public bool ForceCompleted { get; set; }

	public MultiplayerMatchDto Match { get; set; } = null!;
	public ICollection<MultiplayerScoreDto> Scores { get; set; } = [];
}