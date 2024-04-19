using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task ChannelPart(Player player, BinaryReader br)
	{
		var channelName = br.ReadOsuString();

		if (_ignoredChannels.Contains(channelName))
			return Task.CompletedTask;

		var channel = _session.GetChannel(channelName);
		if (channel == null)
		{
			Console.WriteLine($"[ChannelPart] {player.Username} failed to leave {channelName}");
			return Task.CompletedTask;
		}
		
		player.LeaveChannel(channel);
		player.LastActivityTime = DateTime.Now;
		
		return Task.CompletedTask;
	}
}