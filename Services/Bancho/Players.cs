using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	public async Task<Player?> GetPlayerFromLogin(string username, string pwdMD5)
	{
		var player = await GetPlayerOrOffline(username);
		if (player == null) return null;

		return _session.CheckHashes(pwdMD5, player.PasswordHash) ? player : null;
	}
	
	public async Task<Player?> GetPlayerOrOffline(string username)
	{
		var sessionPlayer = _session.GetPlayer(username: username);
		if (sessionPlayer != null) return sessionPlayer;

		username = username.MakeSafe();
		var dbPlayer = await _dbContext.Players.FirstOrDefaultAsync(p => p.SafeName == username);

		return dbPlayer == null ? null : new Player(dbPlayer);
	}
	
	public async Task UpdateLatestActivity(Player player)
	{
		player.LastActivityTime = DateTime.UtcNow;
		
		await _dbContext.Players
		               .Where(p => p.Id == player.Id)
		               .ExecuteUpdateAsync(p => 
			               p.SetProperty(u => u.LastActivityTime, player.LastActivityTime));
	}
	
	public async Task UpdatePlayerCountry(Player player, string country)
	{
		await _dbContext.Players
		               .Where(p => p.Id == player.Id)
		               .ExecuteUpdateAsync(p => 
			               p.SetProperty(u => u.Country, country));
	}
	
	public async Task<PlayerDto?> FetchPlayerInfo(int id = 1, string username = "", string loginName = "")
	{
		if (id > 1)
			return await _dbContext.Players.FindAsync(id);

		if (username != "")
		{
			username = username.MakeSafe();
			return await _dbContext.Players.FirstOrDefaultAsync(p => p.SafeName == username);
		}
		
		if (loginName != "")
		{
			loginName = loginName.ToLogin();
			return await _dbContext.Players.FirstOrDefaultAsync(p => p.LoginName == loginName);
		}

		return null;
	}
	
	public async Task FetchPlayerStats(Player player)
	{
		var stats = await _dbContext.Stats.Where(s => s.PlayerId == player.Id).ToListAsync();
		
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
				ReplayViews = stat.ReplaysViews,
				Grades = { 
					{Grade.XH, stat.XHCount},
					{Grade.X, stat.XCount},
					{Grade.SH, stat.SHCount},
					{Grade.S, stat.SCount},
					{Grade.A, stat.ACount}
				},
				TotalGekis = stat.TotalGekis,
				TotalKatus = stat.TotalKatus,
				Total300s = stat.Total300s,
				Total100s = stat.Total100s,
				Total50s = stat.Total50s
			};
		}
	}
	
	public async Task FetchPlayerRelationships(Player player)
	{
		var relationships = await _dbContext.Relationships.Where(p => p.PlayerId == player.Id).ToListAsync();
		
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
		
		await _dbContext.Players.Where(p => p.Id == player.Id)
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
			LoginName = name.ToLogin(),
			SafeName = name.MakeSafe(),
			Email = email,
			PasswordHash = pwdHash,
			Privileges = 1,
			Country = country,
			CreationTime = DateTime.UtcNow,
			LastActivityTime = DateTime.UtcNow
		};
		
		var player = await _dbContext.Players.AddAsync(playerDto);
		await _dbContext.SaveChangesAsync();

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

		await _dbContext.Stats.AddRangeAsync(scoreDtos);
		await _dbContext.SaveChangesAsync();
	}
}