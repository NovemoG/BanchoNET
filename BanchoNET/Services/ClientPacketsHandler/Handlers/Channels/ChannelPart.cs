using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task ChannelPart(Player player, BinaryReader br)
	{
		var channelName = br.ReadOsuString();

		if (IgnoredChannels.Contains(channelName))
			return Task.CompletedTask;

		var channel = session.GetChannel(channelName);
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