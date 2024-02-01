using BanchoNET.Models;
using BanchoNET.Objects.Channels;

namespace BanchoNET.Services;

public partial class BanchoHandler(BanchoContext context, BanchoSession session)
{
	public Player? GetPlayerSession(string username)
	{
		if (username == "Cossin")
		{
			return new Player
			{
				Username = "Cossin",
				PasswordHash = "test",
				Country = "Poland"
			};
		}
		
		return null;
	}

	public void AppendPlayerSession(Player player)
	{
		session.Players.Add(player);
	}

	public List<Channel> GetAutoJoinChannels(Player player)
	{
		var joinChannels = new List<Channel>();

		foreach (var channel in session.Channels)
		{
			if (!channel.AutoJoin || 
			    channel.Privileges > player.Privileges /*TODO temporary*/ ||
			    channel.Name == "#lobby")
			{
				continue;
			}

			joinChannels.Add(channel);
			
			//TODO Send to all players present in the channel to update their player count
		}

		return joinChannels;
	}
}