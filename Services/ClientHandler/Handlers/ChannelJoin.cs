using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private Task ChannelJoin(Player player, BinaryReader br)
	{
		var name = br.ReadOsuString();

		if (_ignoredChannels.Contains(name))
			return Task.CompletedTask;
		
		var channel = _session.GetChannel(name);

		if (channel == null || !player.JoinChannel(channel))
		{
			//TODO log
			Console.WriteLine($"[ChannelJoin] {player} failed to join {name}");
		}
		
		player.LastActivityTime = DateTime.UtcNow;
		return Task.CompletedTask;
	}
}