using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchScoreUpdate(Player player, BinaryReader br)
	{
		var rawData = br.ReadRawData();
		if (rawData.Length == 0) return Task.CompletedTask;

		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;
		
		var slotId = lobby.GetPlayerSlotId(player);
		
		var bytes = new ServerPackets()
			.MatchScoreUpdate(rawData)
			.FinalizeAndGetContent();

		if (bytes.Length <= 11) return Task.CompletedTask;
		
		bytes[11] = (byte)slotId;
		
		lobby.Enqueue(bytes, toLobby: false);
		return Task.CompletedTask;
	}
}