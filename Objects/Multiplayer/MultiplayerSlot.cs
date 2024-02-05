using BanchoNET.Objects.Players;

namespace BanchoNET.Objects.Multiplayer;

public class MultiplayerSlot
{
	public Player? Player { get; set; }
	public SlotStatus Status { get; set; }
	public LobbyTeams Team { get; set; }
	public Mods Mods { get; set; }
	public bool Loaded { get; set; }
	public bool Skipped { get; set; }
}