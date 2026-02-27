using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task ChannelJoin(User player, BinaryReader br)
	{
		var channelName = br.ReadOsuString();

		if (IgnoredChannels.Contains(channelName))
			return Task.CompletedTask;
		
		var channel = channels.GetChannel(channelName);

		if (channel == null || !channels.JoinPlayer(channel, player))
			Console.WriteLine($"[ChannelJoin] {player.Username} failed to join {channelName}");
		
		player.LastActivityTime = DateTime.UtcNow;
		return Task.CompletedTask;
	}
}