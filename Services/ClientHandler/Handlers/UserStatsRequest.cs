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
			
			if (_session.Bots.TryGetValue(id, out var bot))
			{
				statsPacket.BotStats(bot);
				ids.RemoveAt(i);
			}
			else if (_session.Players.TryGetValue(id, out var user))
			{
				statsPacket.UserStats(user);
				ids.RemoveAt(i);
			}
		}

		player.Enqueue(statsPacket.GetContent());
		return Task.CompletedTask;
	}
}