using BanchoNET.Models;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Services;

public class BanchoSession
{
	public BanchoSession()
	{
		Channels = new List<Channel>
		{
			new()
			{
				Id = 0,
				Name = "#osu",
				Description = "Main osu! chatroom",
				AutoJoin = true,
				Hidden = false,
				ReadOnly = false,
				Privileges = (int)ClientPrivileges.PLAYER,
				Players = []
			},
			new()
			{
				Id = 1,
				Name = "#lobby",
				Description = "Multiplayer chatroom",
				AutoJoin = false,
				Hidden = false,
				ReadOnly = false,
				Privileges = (int)ClientPrivileges.PLAYER,
				Players = []
			}
		};

		Players = new List<Player>
		{
			//TODO bancho bot
			new()
			{
				Id = 1,
				Username = "Bancho",
				LastConnectionTime = DateTime.MaxValue,
				Privileges = (int)Privileges.UNRESTRICTED,
				BotClient = true
			}
		};
	}
	
	public readonly List<Channel> Channels;
	public readonly List<Player> Players; //TODO maybe Players class with bots, players, restricted collections
}