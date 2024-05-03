using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task CreateMatch(Player player, BinaryReader br)
	{
		var matchData = br.ReadOsuMatch();
		matchData.CreatorId = matchData.HostId;
		
		if (player.Restricted)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while restricted.");
			player.Enqueue(joinFailPacket.GetContent());
			return;
		}

		if (player.Silenced)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while silenced.");
			player.Enqueue(joinFailPacket.GetContent());
			return;
		}
		
		MultiplayerExtensions.CreateLobby(matchData, player, await histories.GetMatchId());
		player.LastActivityTime = DateTime.Now;
	}
}