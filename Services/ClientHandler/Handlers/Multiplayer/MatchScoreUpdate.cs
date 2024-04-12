using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private Task MatchScoreUpdate(Player player, BinaryReader br)
	{
		var length = br.BaseStream.Length;
		Console.WriteLine($"Length of match score data: {length}, currently at: {length - br.BaseStream.Position}");
		
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		if (lobby.Mods.HasMod(Mods.ScoreV2))
			br.ReadBytes(8);
		
		var slotId = lobby.GetPlayerSlotId(player);
		lobby.Enqueue([0], toLobby: false);
		
		return Task.CompletedTask;
	}
}