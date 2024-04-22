using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchScoreUpdate(Player player, BinaryReader br)
	{
		var rawData = br.ReadRawData();
		
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;
		
		var slotId = lobby.GetPlayerSlotId(player);
		
		using var scoreUpdatePacket = new ServerPackets();
		scoreUpdatePacket.MatchScoreUpdate(rawData);
		var returnData = scoreUpdatePacket.GetContent();
		
		returnData[11] = (byte)slotId;
		
		lobby.Enqueue(returnData, toLobby: false);
		return Task.CompletedTask;
	}
}