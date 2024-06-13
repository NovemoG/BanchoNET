using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using BanchoNET.Models.Mongo;
using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Objects.Scores;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver.Linq;
using StackExchange.Redis;

namespace BanchoNET.Services.Repositories;

public class PlayersRepository
{
	private readonly BanchoSession _session = BanchoSession.Instance;
	private readonly BanchoDbContext _dbContext;
	private readonly IDatabase _redis;
	private readonly HistoriesRepository _histories;
	
	public PlayersRepository(
		BanchoDbContext dbContext,
		IConnectionMultiplexer redis,
		HistoriesRepository histories)
	{
		_dbContext = dbContext;
		_redis = redis.GetDatabase();
		_histories = histories;
	}
	
	public async Task<bool> EmailTaken(string email)
	{
		return await _dbContext.Players.AnyAsync(p => p.Email == email);
	}
	
	public async Task<bool> UsernameTaken(string username)
	{
		return await _dbContext.Players.AnyAsync(p => p.SafeName == username.MakeSafe());
	}

	public async Task AddFriend(Player player, int targetId)
	{
		if (player.Friends.Contains(targetId))
			return;
		
		player.Friends.Add(targetId);
		
		await _dbContext.Relationships.AddAsync(new RelationshipDto
		{
			PlayerId = player.Id,
			TargetId = targetId,
			Relation = (byte)Relations.Friend
		});
		await _dbContext.SaveChangesAsync();
	}
	
	public async Task RemoveFriend(Player player, int targetId)
	{
		if (!player.Friends.Contains(targetId))
			return;
		
		player.Friends.Remove(targetId);

		await _dbContext.Relationships
			.Where(r => r.PlayerId == player.Id && r.TargetId == targetId)
			.ExecuteDeleteAsync();
	}

	public async Task<Player?> GetPlayerFromLogin(string username, string pwdMD5)
	{
		var player = await GetPlayerOrOffline(username);
		if (player == null) return null;

		return _session.CheckHashes(pwdMD5, player.PasswordHash) ? player : null;
	}
	
	public async Task<Player?> GetPlayerOrOffline(string username)
	{
		var sessionPlayer = _session.GetPlayerByName(username);
		if (sessionPlayer != null) return sessionPlayer;
		
		var dbPlayer = await _dbContext.Players.FirstOrDefaultAsync(p => p.SafeName == username.MakeSafe());

		return dbPlayer == null ? null : new Player(dbPlayer);
	}
	public async Task<Player?> GetPlayerOrOffline(int id)
	{
		var sessionPlayer = _session.GetPlayerById(id);
		if (sessionPlayer != null) return sessionPlayer;
		
		var dbPlayer = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == id);

		return dbPlayer == null ? null : new Player(dbPlayer);
	}
	
	public async Task UpdateLatestActivity(Player player)
	{
		player.LastActivityTime = DateTime.Now;
		
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
			return await _dbContext.Players.FirstOrDefaultAsync(p => p.SafeName == username.MakeSafe());
		
		if (loginName != "")
			return await _dbContext.Players.FirstOrDefaultAsync(p => p.LoginName == loginName.MakeSafe());

		return null;
	}
	
	public async Task FetchPlayerStats(Player player)
	{
		var stats = await _dbContext.Stats.Where(s => s.PlayerId == player.Id).ToListAsync();
		
		foreach (var stat in stats)
		{
			var mode = (GameMode)stat.Mode;
			
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
			PeakRank = stats.PeakRank,
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
	
	public async Task ModifyPlayerPrivileges(Player player, Privileges privileges, bool remove)
	{
		if (remove)
			player.Privileges &= ~privileges;
		else
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
				await InsertPlayerGlobalRank((byte)mode, country, player.Id, stats.PP);
				break;
			case true:
				stats.Rank = 0;
				return;
		}
		
		stats.Rank = await GetPlayerGlobalRank(mode, player.Id);
		if (stats.Rank > stats.PeakRank)
		{
			stats.PeakRank = stats.Rank;
			await _histories.UpdatePeakRank(
				player.Id,
				(byte)mode,
				new PeakRank
				{
					Value = stats.PeakRank,
					Date = DateTime.Now
				});
		}
	}
	
	public async Task CreatePlayer(string name, string email, string passwordHash, string country)
	{
		var playerDto = new PlayerDto
		{
			Username = name,
			LoginName = name.MakeSafe(),
			SafeName = name.MakeSafe(),
			Email = email,
			PasswordHash = passwordHash,
			Privileges = 1,
			Country = country,
			CreationTime = DateTime.Now,
			LastActivityTime = DateTime.Now
		};
		
		var player = await _dbContext.Players.AddAsync(playerDto);
		await _dbContext.SaveChangesAsync();

		var playerId = player.Entity.Id;
		
		await _dbContext.Relationships.AddAsync(new RelationshipDto
		{
			PlayerId = playerId,
			TargetId = 1,
			Relation = (byte)Relations.Friend
		});
		
		var scoreDtos = new StatsDto[8];
		for (byte i = 0; i < scoreDtos.Length; i++)
		{
			var mode = i == 7 ? (byte)(i + 1) : i;
			
			scoreDtos[i] = new StatsDto
			{
				PlayerId = playerId,
				Mode = mode
			};
			
			await _redis.SortedSetAddAsync($"bancho:leaderboard:{mode}", playerId, 0);

			var rank = await GetPlayerGlobalRank((GameMode)mode, playerId);

			await Task.WhenAll(
				_histories.InsertRankHistory(new RankHistory
				{
					PlayerId = playerId,
					Mode = mode,
					PeakRank = new PeakRank
					{
						Value = rank,
						Date = DateTime.Now
					},
					Entries = [rank]
				}),
				_histories.InsertReplaysHistory(new ReplayViewsHistory
				{
					PlayerId = playerId,
					Mode = mode,
					Entries = [0]
				}),
				_histories.InsertPlayCountHistory(new PlayCountHistory
				{
					PlayerId = playerId,
					Mode = mode,
					Entries = [0]
				}));
		}

		await _dbContext.Stats.AddRangeAsync(scoreDtos);
		await _dbContext.SaveChangesAsync();
	}

	public async Task<bool> DeletePlayer(int playerId, bool deleteScores)
	{
		var online = _session.GetPlayerById(playerId);
		if (online != null) return false;
		
		var player = await _dbContext.Players.FindAsync(playerId);
		if (player == null) return false;

		var batch = _redis.CreateBatch();

		for (byte i = 0; i < 8; i++)
		{
			var mode = i == 7 ? (byte)(i + 1) : i;

			await batch.SortedSetRemoveAsync($"bancho:leaderboard:{mode}", playerId);
			await batch.SortedSetRemoveAsync($"bancho:leaderboard:{mode}:{player.Country}", playerId);
		}

		batch.Execute();

		if (deleteScores)
			await _dbContext.Scores.Where(s => s.PlayerId == playerId).ExecuteDeleteAsync();
		await _dbContext.Relationships.Where(r => r.PlayerId == playerId || r.TargetId == playerId).ExecuteDeleteAsync();
		await _dbContext.Stats.Where(s => s.PlayerId == playerId).ExecuteDeleteAsync();
		await _dbContext.Players.Where(p => p.Id == playerId).ExecuteDeleteAsync();

		await _histories.DeletePlayerData(playerId);

		return true;
	}

	public async Task<StatsDto?> GetPlayerModeStats(int playerId, byte mode)
	{
		return await _dbContext.Stats.FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Mode == mode);
	}

	public async Task<bool> SilencePlayer(Player player, TimeSpan duration, string reason)
	{
		var modified = await _dbContext.Players.Where(p => p.Id == player.Id)
			.ExecuteUpdateAsync(s => s.SetProperty(p => p.RemainingSilence, DateTime.Now + duration));
		
		using var silenceEndPacket = new ServerPackets();
		silenceEndPacket.SilenceEnd((int)duration.TotalSeconds);
		player.Enqueue(silenceEndPacket.GetContent());
		
		using var userSilencedPacket = new ServerPackets();
		userSilencedPacket.UserSilenced(player.Id);
		_session.EnqueueToPlayers(userSilencedPacket.GetContent());

		if (player.InMatch)
			player.LeaveMatch();

		return modified == 1;
	}
	
	public async Task<bool> UnsilencePlayer(Player player, string reason)
	{
		var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
		if (entity == null) return false;
		
		entity.RemainingSilence = DateTime.Now;
		await _dbContext.SaveChangesAsync();
		
		using var silenceEndPacket = new ServerPackets();
		silenceEndPacket.SilenceEnd(0);
		player.Enqueue(silenceEndPacket.GetContent());

		return true;
	}

	public async Task<bool> RestrictPlayer(Player player, string reason)
	{
		var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
		if (entity == null) return false;
		
		//TODO log reason to database
		
		entity.Privileges &= ~(int)Privileges.Unrestricted;
		await _dbContext.SaveChangesAsync();

		for (byte i = 0; i < 8; i++)
		{
			var mode = i == 7 ? (byte)(i + 1) : i;

			await RemovePlayerGlobalRank(mode, player.Geoloc.Country.Acronym, player.Id);
		}

		_session.LogoutPlayer(player);

		return true;
	}

	public async Task<bool> UnrestrictPlayer(Player player, string reason)
	{
		var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
		if (entity == null) return false;
		
		entity.Privileges |= (int)Privileges.Unrestricted;
		await _dbContext.SaveChangesAsync();

		if (!player.Online)
			await FetchPlayerStats(player);

		foreach (var stats in player.Stats)
			await InsertPlayerGlobalRank((byte)stats.Key, player.Geoloc.Country.Acronym, player.Id, stats.Value.PP);
		
		_session.LogoutPlayer(player);

		return true;
	}
	
	/// <summary>
	/// Returns a list of tuples that contains: player id, play count and replay views of players in a given mode.
	/// At the same time it resets the play count and replay views of the players as it is our most recent data.
	/// </summary>
	public async Task<List<Tuple<int, int, int>>> GetPlayerModeStatsInRange(byte mode, int count, int skip = 0)
	{
		if (count % 8 != 0 || skip % 8 != 0)
			Console.WriteLine($"[Players] Taking/Skip count should be a multiple of 8, count: {count}, skip: {skip}");

		return await _dbContext.Stats
			.Where(s => s.Mode == mode)
			.OrderBy(s => s.PlayerId)
			.Skip(skip)
			.Take(count)
			.Select(s => new Tuple<int, int, int>(s.PlayerId, s.PlayCount, s.ReplayViews))
			.ToListAsync();
	}

	public async Task ResetPlayersStats(byte mode)
	{
		await _dbContext.Stats.Where(s => s.Mode == mode)
			.ExecuteUpdateAsync(st => st.SetProperty(s => s.PlayCount, 0)
				.SetProperty(s => s.ReplayViews, 0));
	}

	/// <summary>
	/// Returns the total count of players that are not restricted.
	/// </summary>
	public async Task<int> TotalPlayerCount()
	{
		return await _dbContext.Players
			.Where(p => (p.Privileges & 1) == 1)
			.CountAsync();
	}

	/// <summary>
	/// Returns a list of player IDs with expired supporter status and updates their privileges.
	/// </summary>
	/// <returns>List of player IDs with expired supporter</returns>
	public async Task<List<int>> GetPlayersWithExpiredSupporter()
	{
		var query = _dbContext.Players
			.Where(p => p.RemainingSupporter < DateTime.Now
			            && (p.Privileges & (int)Privileges.Supporter) == (int)Privileges.Supporter);
		
		// Saving IDs before update
		var playerIds = await query.Select(p => p.Id).ToListAsync();

		// Updating supporter status
		await query.ExecuteUpdateAsync(s => s.SetProperty(p => p.RemainingSupporter, DateTime.MinValue)
			.SetProperty(p => p.Privileges, e => e.Privileges & ~(int)Privileges.Supporter));
		
		return playerIds;
	}

	public async Task<int> GetPlayerGlobalRank(GameMode mode, int playerId)
	{
		return (int)(await _redis.SortedSetRankAsync($"bancho:leaderboard:{(byte)mode}", playerId, Order.Descending))! + 1;
	}

	public async Task InsertPlayerGlobalRank(byte mode, string country, int playerId, ushort pp)
	{
		await _redis.SortedSetAddAsync($"bancho:leaderboard:{mode}", playerId, pp);
		await _redis.SortedSetAddAsync($"bancho:leaderboard:{mode}:{country}", playerId, pp);
	}

	public async Task RemovePlayerGlobalRank(byte mode, string country, int playerId)
	{
		await _redis.SortedSetRemoveAsync($"bancho:leaderboard:{mode}", playerId);
		await _redis.SortedSetRemoveAsync($"bancho:leaderboard:{mode}:{country}", playerId);
	}
}