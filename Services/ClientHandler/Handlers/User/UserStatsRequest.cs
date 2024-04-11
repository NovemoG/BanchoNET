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

		Console.Write("[UserStatsRequest] User requested ids: ");
		foreach (var id in ids)
		{
			Console.Write($"{id}, ");
		}
		Console.Write('\n');

		for (var i = ids.Count - 1; i >= 0; i--)
		{
			var id = ids[i];

			var bot = _session.GetBot(id: id);
			if (bot != null)
			{
				statsPacket.BotStats(bot);
				ids.RemoveAt(i);
			}
			else
			{
				var user = _session.GetPlayer(id: id);
				if (user == null) continue;
				
				statsPacket.UserStats(user);
				ids.RemoveAt(i);
			}
		}

		player.Enqueue(statsPacket.GetContent());
		player.LastActivityTime = DateTime.Now;
		
		return Task.CompletedTask;
	}
}