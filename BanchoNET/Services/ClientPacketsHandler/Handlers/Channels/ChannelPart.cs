using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task ChannelPart(User player, BinaryReader br)
	{
		var channelName = br.ReadOsuString();

		if (IgnoredChannels.Contains(channelName))
			return Task.CompletedTask;

		var channel = channels.GetChannel(channelName);
		if (channel == null)
		{
			Console.WriteLine($"[ChannelPart] {player.Username} failed to leave {channelName}");
			return Task.CompletedTask;
		}

		channels.LeavePlayer(channel, player);
		player.LastActivityTime = DateTime.UtcNow;
		
		return Task.CompletedTask;
	}
}