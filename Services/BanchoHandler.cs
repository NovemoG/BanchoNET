using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services;

public partial class BanchoHandler(BanchoDbContext dbContext, BanchoSession session)
{
	public void AppendPlayerSession(Player player)
	{
		session.Players[player.Id] = player;
	}
	
	public void RemovePlayerSession(int id)
	{
		session.Players.Remove(id);
	}

	public async Task PlayerLogout(Player player)
	{
		if (player.Lobby != null)
		{
			player.LeaveMatch();
		}

		player.Spectating?.RemoveSpectator();

		while (player.Channels.Count != 0)
		{
			player.LeaveChannel(player.Channels[0]);
		}

		RemovePlayerSession(player.Id);

		if (!player.Restricted)
		{
			using var logoutPacket = new ServerPackets();
			logoutPacket.Logout(player.Id);
			
			session.EnqueueToPlayers();
		}
	}

	public Player? GetPlayerSession(int id = 1, string username = "", Guid token = new())
	{
		if (id > 1 && session.Players.TryGetValue(id, out var value))
		{
			return value;
		}

		if (username != "")
		{
			foreach (var player in session.Players.Where(player => player.Value.Username == username))
			{
				return player.Value;
			}
		}

		if (token != Guid.Empty)
		{
			foreach (var player in session.Players.Where(player => player.Value.Token == token))
			{
				return player.Value;
			}
		}

		return null;

		/*if (id < 2 && username == "" && token == "") return null;

		foreach (var player in session.Players)
		{
			if (player.Id == id) return player;
			if (player.Username == username) return player;
			if (player.Token == token) return player;
		}

		return null;*/
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

	public async Task<PlayerDto?> FetchPlayerInfo(int id = 1, string username = "", string loginName = "")
	{
		if (id > 1)
			return await dbContext.Players.FindAsync(id);

		if (username != "")
			return await dbContext.Players.SingleOrDefaultAsync(p => p.SafeName == username.MakeSafe());

		if (loginName != "")
			return await dbContext.Players.SingleOrDefaultAsync(p => p.LoginName == loginName);;

		return null;
	}
	
	public async Task FetchPlayerStats(Player player)
	{
		var stats = await (from stat in dbContext.Stats
			where stat.PlayerId == player.Id
			select stat).ToListAsync();
		
		foreach (var stat in stats)
		{
			player.Stats[(GameMode)stat.Mode] = new ModeStats
			{
				TotalScore = stat.TotalScore,
				RankedScore = stat.RankedScore,
				PP = stat.PP,
				Accuracy = stat.Accuracy,
				PlayCount = stat.PlayCount,
				PlayTime = stat.PlayTime,
				MaxCombo = stat.MaxCombo,
				Grades = { 
					{Grades.XH, stat.XHCount},
					{Grades.X, stat.XCount},
					{Grades.SH, stat.SHCount},
					{Grades.S, stat.SCount},
					{Grades.A, stat.ACount}
				}
			};
		}
	}
	
	public async Task FetchPlayerRelationships(Player player)
	{
		var relationships = await
			(from relationship in dbContext.Relationships
			where relationship.UserId == player.Id
			select relationship).ToListAsync();

		foreach (var relationship in relationships)
		{
			switch (relationship.Type)
			{
				case 0:
				case 1:
					player.Friends.Add(relationship.TargetId);
					break;
				case 2:
					player.Blocked.Add(relationship.TargetId);
					break;
			}
		}
	}
	
	public async Task CreatePlayer(string name, string email, string pwdHash, string country)
	{
		var playerDto = new PlayerDto
		{
			Username = name,
			LoginName = name,
			SafeName = name.MakeSafe(),
			Email = email,
			PasswordHash = pwdHash,
			Country = country,
			CreationTime = DateTime.UtcNow,
			LastActivityTime = DateTime.UtcNow
		};
		
		var player = await dbContext.Players.AddAsync(playerDto);
		await dbContext.SaveChangesAsync();

		var playerId = player.Entity.Id;
		
		//TODO Create only for standard?
		var scoreDtos = new StatsDto[8];
		for (byte i = 0; i < scoreDtos.Length; i++)
		{
			scoreDtos[i] = new StatsDto
			{
				PlayerId = playerId,
				Mode = i == 7 ? (byte)(i + 1) : i
			};
		}

		await dbContext.Stats.AddRangeAsync(scoreDtos);
		await dbContext.SaveChangesAsync();
	}
	
	public async Task<bool> EmailTaken(string email)
	{
		return await dbContext.Players.AnyAsync(player => player.Email == email);
	}
	
	public async Task<bool> UsernameTaken(string username)
	{
		return await dbContext.Players.AnyAsync(player => player.SafeName == username.MakeSafe());
	}
}