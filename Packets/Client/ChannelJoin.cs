using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Packets;

public partial class ClientPackets
{
	private static void ChannelJoin(Player player, BinaryReader br)
	{
		var name = br.ReadOsuString();
		
		if (IgnoredChannels.Contains(name))
			return;
		
		var channel = Session.GetChannel(name);

		if (channel == null || !player.JoinChannel(channel))
		{
			//TODO log
			Console.WriteLine($"{player} failed to join {channel.Name}");
		}
	}
}