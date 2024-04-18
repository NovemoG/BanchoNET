using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private Task UserStatsRequest(Player player, BinaryReader br)
	{
		var ids = br.ReadOsuList32();
		
		using var statsPacket = new ServerPackets();

		for (var i = ids.Count - 1; i >= 0; i--)
		{
			var id = ids[i];

			var user = _session.GetPlayer(id: id);
			if (user == null) continue;
			
			if (user.IsBot)
			{
				statsPacket.BotStats(user);
				ids.RemoveAt(i);
			}
			else
			{
				statsPacket.UserStats(user);
				ids.RemoveAt(i);
			}
		}

		player.Enqueue(statsPacket.GetContent());
		player.LastActivityTime = DateTime.Now;
		
		return Task.CompletedTask;
	}
}