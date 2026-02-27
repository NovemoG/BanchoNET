using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchScoreUpdate(User player, BinaryReader br)
	{
		var rawData = br.ReadRawData();
		if (rawData.Length == 0) return Task.CompletedTask;

		var match = player.Match;
		if (match == null) return Task.CompletedTask;
		
		var slotId = match.GetPlayerSlotId(player);
		
		var bytes = new ServerPackets()
			.MatchScoreUpdate(rawData)
			.FinalizeAndGetContent();

		if (bytes.Length <= 11) return Task.CompletedTask;
		
		bytes[11] = (byte)slotId;
		
		multiplayerCoordinator.EnqueueTo(match, bytes, toLobby: false);
		return Task.CompletedTask;
	}
}