using BanchoNET.Objects.Players;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task ChannelJoin(Player player, BinaryReader br)
	{
		var channelName = br.ReadOsuString();

		if (_ignoredChannels.Contains(channelName))
			return Task.CompletedTask;
		
		var channel = _session.GetChannel(channelName);

		if (channel == null || !player.JoinChannel(channel))
			Console.WriteLine($"[ChannelJoin] {player.Username} failed to join {channelName}");
		
		player.LastActivityTime = DateTime.Now;
		return Task.CompletedTask;
	}
}