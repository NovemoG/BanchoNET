using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

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
			loginName = loginName.MakeSafe();
			return await _dbContext.Players.FirstOrDefaultAsync(p => p.LoginName == loginName);
		}

		return null;
	}
	
	public async Task FetchPlayerStats(Player player)
	{
		var stats = await _dbContext.Stats.Where(s => s.PlayerId == player.Id).ToListAsync();
		
		foreach (var stat in stats)
		{
			var mode = (GameMode)stat.Mode;
			
			await _redis.SortedSetAddAsync($"bancho:leaderboard:{(byte)mode}", player.Id, stat.PP);
			
			player.Stats[mode] = new ModeStats
			{
				TotalScore = stat.TotalScore,
				RankedScore = stat.RankedScore,
				PP = stat.PP,
				Accuracy = stat.Accuracy,
				PlayCount = stat.PlayCount,
				PlayTime = stat.PlayTime,
				MaxCombo = stat.MaxCombo,
				ReplayViews = stat.ReplayViews,
				Rank = await GetPlayerGlobalRank(mode, player.Id),
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

	public async Task UpdatePlayerStats(Player player, GameMode mode)
	{
		var stats = player.Stats[mode];
		
		var dbStats = new StatsDto
		{
			PlayerId = player.Id,
			Mode = (byte)mode,
			TotalScore = stats.TotalScore,
			RankedScore = stats.RankedScore,
			PP = stats.PP,
			Accuracy = stats.Accuracy,
			PlayCount = stats.PlayCount,
			PlayTime = stats.PlayTime,
			MaxCombo = stats.MaxCombo,
			ReplayViews = stats.ReplayViews,
			XHCount = stats.Grades[Grade.XH],
			XCount = stats.Grades[Grade.X],
			SHCount = stats.Grades[Grade.SH],
			SCount = stats.Grades[Grade.S],
			ACount = stats.Grades[Grade.A],
			TotalGekis = stats.TotalGekis,
			TotalKatus = stats.TotalKatus,
			Total300s = stats.Total300s,
			Total100s = stats.Total100s,
			Total50s = stats.Total50s
		};
		_dbContext.Update(dbStats);
		await _dbContext.SaveChangesAsync();
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

	public async Task RecalculatePlayerTopScores(Player player, GameMode mode)
	{
		var bestScores = await _dbContext.Scores.Where(s => s.PlayerId == player.Id &&
		                                                    s.Status == (byte)SubmissionStatus.Best)
		                                 .OrderByDescending(s => s.PP)
		                                 .Take(100)
		                                 .ToListAsync();

		var weightedAcc = 0.0f;
		var weightedPp = 0.0f;

		for (var i = 0; i < bestScores.Count; i++)
		{
			var score = bestScores[i];
			var weight = MathF.Pow(0.95f, i);

			weightedAcc += score.Acc * weight;
			weightedPp += score.PP * weight;
		}

		var accWeight = 100f / (20 * (1 - MathF.Pow(0.95f, bestScores.Count)));
		var bonusPp = (417 - (float)1/3) * (1 - MathF.Pow(0.995f, MathF.Min(1000, bestScores.Count)));
		
		var stats = player.Stats[mode];
		stats.Accuracy = weightedAcc * accWeight / 100;
		stats.PP = (ushort)MathF.Round(weightedPp + bonusPp);
	}

	public async Task UpdatePlayerRank(Player player, GameMode mode)
	{
		var country = player.Geoloc.Country.Acronym;
		var stats = player.Stats[mode];

		switch (player.Restricted)
		{
			case false:
				await _redis.SortedSetAddAsync($"bancho:leaderboard:{(byte)mode}", player.Id, stats.PP);
				await _redis.SortedSetAddAsync($"bancho:leaderboard:{(byte)mode}:{country}", player.Id, stats.PP);
				break;
			case true:
				stats.Rank = 0;
				return;
		}

		stats.Rank = await GetPlayerGlobalRank(mode, player.Id);
	}
	
	public async Task CreatePlayer(string name, string email, string pwdHash, string country)
	{
		var playerDto = new PlayerDto
		{
			Username = name,
			LoginName = name.MakeSafe(),
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

	private async Task<int> GetPlayerGlobalRank(GameMode mode, int playerId)
	{
		return (int)(await _redis.SortedSetRankAsync($"bancho:leaderboard:{(byte)mode}", playerId, Order.Descending))! + 1;
	}
}