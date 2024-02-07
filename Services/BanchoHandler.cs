using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services;

public partial class BanchoHandler(BanchoDbContext dbContext)
{
	public async Task<PlayerDto?> FetchPlayerInfo(int id = 1, string username = "", string loginName = "")
	{
		if (id > 1)
			return await dbContext.Players.FindAsync(id);

		if (username != "")
			return await dbContext.Players.SingleOrDefaultAsync(p => p.SafeName == username.MakeSafe());

		//TODO login name can only contain alphanumeric characters
		if (loginName != "")
			return await dbContext.Players.SingleOrDefaultAsync(p => p.LoginName == loginName);;

		return null;
	}
	
	public async Task FetchPlayerStats(Player player)
	{
		var stats = await dbContext.Stats.Where(s => s.PlayerId == player.Id).ToListAsync();
		
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
					{Grade.XH, stat.XHCount},
					{Grade.X, stat.XCount},
					{Grade.SH, stat.SHCount},
					{Grade.S, stat.SCount},
					{Grade.A, stat.ACount}
				}
			};
		}
	}
	
	public async Task FetchPlayerRelationships(Player player)
	{
		var relationships = await dbContext.Relationships.Where(p => p.PlayerId == player.Id).ToListAsync();
		
		foreach (var relationship in relationships)
		{
			switch ((Relations)relationship.Relation)
			{
				case Relations.Friend:
					player.Friends.Add(relationship.TargetId);
					break;
				case Relations.Block:
					player.Blocked.Add(relationship.TargetId);
					break;
			}
		}
	}
	
	public async Task AddPlayerPrivileges(Player player, Privileges privileges)
	{
		player.Privileges |= privileges;
		
		await dbContext.Players.Where(p => p.Id == player.Id)
		               .ExecuteUpdateAsync(p => 
			               p.SetProperty(u => u.Privileges, (int)player.Privileges));

		if (player.Online)
		{
			using var privPacket = new ServerPackets();
			privPacket.BanchoPrivileges((int)player.ToBanchoPrivileges());
			player.Enqueue(privPacket.GetContent());
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
			Privileges = 1,
			Country = country,
			CreationTime = DateTime.UtcNow,
			LastActivityTime = DateTime.UtcNow
		};
		
		var player = await dbContext.Players.AddAsync(playerDto);
		await dbContext.SaveChangesAsync();

		var playerId = player.Entity.Id;
		
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
		return await dbContext.Players.AnyAsync(p => p.Email == email);
	}
	
	public async Task<bool> UsernameTaken(string username)
	{
		return await dbContext.Players.AnyAsync(p => p.SafeName == username.MakeSafe());
	}
}