using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task UserStatsRequest(Player player, BinaryReader br)
	{
		var ids = br.ReadOsuListInt32();
		ids.Remove(player.Id);
		
		using var statsPacket = new ServerPackets();

		foreach (var id in ids)
		{
			var user = _session.GetPlayer(id: id);
			if (user == null) continue;
			
			if (user.IsBot) statsPacket.BotStats(user);
			else statsPacket.UserStats(user);
		}

		player.Enqueue(statsPacket.GetContent());
		
		return Task.CompletedTask;
	}
}