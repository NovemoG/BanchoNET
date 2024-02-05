using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Services;

public class BanchoSession
{
	public readonly List<Channel> Channels;

	public readonly Dictionary<int, Player> Players;
	//public readonly List<Player> Players;
	public readonly List<Player> Restricted;
	public readonly List<Player> Bots;
	
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

		Players = [];
		Restricted = [];

		Bots = new List<Player>
		{
			new(new PlayerDto{Id = 1}, "")
			{
				Username = "Bancho",
				LastActivityTime = DateTime.MaxValue,
				Privileges = (int)Privileges.UNRESTRICTED,
			}
		};
		/*new PlayerDto
		{
			Id = 1,
			Username = "Bancho",
			TimeZone = 1,
			Country = "Satellite",
			Privileges = 31,
			BotClient = true,
			Longitude = 1234,
			Latitude = 1234,
			Rank = 0,
		})*/
	}

	public void EnqueueToPlayers()
	{
		
	}
}