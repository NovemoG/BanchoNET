using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Packets;

public partial class ClientPackets
{
	private static void UserStatsRequest(Player player, BinaryReader br)
	{
		var ids = br.ReadOsuList16();
		
		using var statsPacket = new ServerPackets();

		Console.Write("User requested ids: ");
		foreach (var id in ids)
		{
			Console.Write($"{id}, ");
		}
		Console.Write('\n');

		//TODO optimize this
		foreach (var bot in Session.Bots)
		{
			if (!ids.Contains(bot.Id)) continue;
			
			statsPacket.BotStats(bot);
		}
		
		foreach (var user in Session.Players)
		{
			if (!ids.Contains(user.Id) || user.Id == player.Id) continue;
			
			statsPacket.UserStats(user);
		}
		
		player.Enqueue(statsPacket.GetContent());
	}
}