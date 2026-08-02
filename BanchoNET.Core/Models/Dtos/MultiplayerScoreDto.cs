using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Models.Stable.Multiplayer;

namespace BanchoNET.Core.Models.Dtos;

public class MultiplayerScoreDto
{
	public long GameId { get; set; }
	public int PlayerId { get; set; }

	/// <summary>
	/// The submitted score this came from, or null once that score has been deleted.
	/// </summary>
	public long? ScoreId { get; set; }

	public LobbyTeams Team { get; set; }

	public long TotalScore { get; set; }
	public int MaxCombo { get; set; }
	public float Accuracy { get; set; }
	public Grade Grade { get; set; }
	public LegacyMods Mods { get; set; }

	public int Count300 { get; set; }
	public int Count100 { get; set; }
	public int Count50 { get; set; }
	public int Gekis { get; set; }
	public int Katus { get; set; }
	public int Misses { get; set; }

	public bool Failed { get; set; }

	public MultiplayerGameDto Game { get; set; } = null!;
	public PlayerDto Player { get; set; } = null!;
}