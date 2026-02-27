using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BanchoNET.Services.Repositories;

public class PlayersRepository : IPlayersRepository
{
	private readonly BanchoDbContext _dbContext;
	private readonly IPlayerService _players;
	private readonly IPlayerCoordinator _playerCoordinator;
	private readonly IMultiplayerCoordinator _multiplayer;
	private readonly IDatabase _redis;
	private readonly IHistoriesRepository _histories;
	
	public PlayersRepository(
		BanchoDbContext dbContext,
		IPlayerService players,
		IPlayerCoordinator playerCoordinator,
		IMultiplayerCoordinator multiplayer,
		IConnectionMultiplexer redis,
		IHistoriesRepository histories)
	{
		_players = players;
		_playerCoordinator = playerCoordinator;
		_multiplayer = multiplayer;
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

	public async Task<bool> PlayerExists(string username)
	{
		return await _dbContext.Players.AnyAsync(p => p.SafeName == username.MakeSafe());
	}

	public async Task<bool> ChangeUsername(string oldUsername, string newUsername){
		
		// MakeSafe just to be sure
		var result = await _dbContext.Players.Where(p => p.SafeName == oldUsername.MakeSafe())
			.ExecuteUpdateAsync(p => 
				p.SetProperty(x => x.Username, newUsername)
					.SetProperty(x => x.SafeName, newUsername.MakeSafe())
					.SetProperty(x => x.LoginName, newUsername.MakeSafe()));

		return result > 0;
	}

	public async Task<List<string>> GetPlayerNames(List<int> ids)
	{
		return await _dbContext.Players
			.Where(p => ids.Contains(p.Id))
			.Select(p => p.Username)
			.ToListAsync();
	}

	public async Task AddFriend(User player, int targetId)
	{
		if (player.Friends.Contains(targetId))
			return;
		
		player.Friends.Add(targetId);
		
		await _dbContext.Relationships.AddAsync(new RelationshipDto
		{
			PlayerId = player.Id,
			TargetId = targetId,
			Relation = (int)Relations.Friend
		});
		await _dbContext.SaveChangesAsync();
	}
	
	public async Task RemoveFriend(User player, int targetId)
	{
		if (!player.Friends.Contains(targetId))
			return;
		
		player.Friends.Remove(targetId);

		await _dbContext.Relationships
			.Where(r => r.PlayerId == player.Id && r.TargetId == targetId)
			.ExecuteDeleteAsync();
	}
	
	public async Task<User?> GetPlayerFromLogin(string username, string passwordMD5)
	{
		var player = await GetPlayerOrOffline(username);
		if (player == null) return null;

		return passwordMD5.VerifyPassword(player.PasswordHash) ? player : null;
	}
	
	public async Task<User?> GetPlayerOrOffline(string username)
	{
		var sessionPlayer = _players.GetPlayer(username);
		if (sessionPlayer != null) return sessionPlayer;
		
		var dbPlayer = await _dbContext.Players.FirstOrDefaultAsync(p => p.SafeName == username.MakeSafe());

		return dbPlayer == null ? null : new User(dbPlayer);
	}
	
	public async Task<User?> GetPlayerOrOffline(int playerId)
	{
		var sessionPlayer = _players.GetPlayer(playerId);
		if (sessionPlayer != null) return sessionPlayer;
		
		var dbPlayer = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId);

		return dbPlayer == null ? null : new User(dbPlayer);
	}
	
	public async Task UpdateLatestActivity(User player)
	{
		player.LastActivityTime = DateTime.UtcNow;
		
		await UpdateLatestActivity(player.Id);
	}
	
	public async Task UpdateLatestActivity(int playerId)
	{
		await _dbContext.Players
			.Where(p => p.Id == playerId)
			.ExecuteUpdateAsync(p => 
				p.SetProperty(u => u.LastActivityTime, DateTime.UtcNow)
				 .SetProperty(u => u.Inactive, false));
	}
	
	public async Task UpdatePlayerCountry(User player, string country)
	{
		await _dbContext.Players
			.Where(p => p.Id == player.Id)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(u => u.Country, country));
	}
	
	public async Task<PlayerDto?> GetPlayerInfo(int playerId)
	{
		if (playerId <= 1) return null;
		
		return await _dbContext.Players.FindAsync(playerId);
	}
	
	public async Task<PlayerDto?> GetPlayerInfo(string username)
	{
		if (string.IsNullOrEmpty(username)) return null;
		
		return await _dbContext.Players.FirstOrDefaultAsync(p => p.SafeName == username.MakeSafe());
	}
	
	public async Task<PlayerDto?> GetPlayerInfoFromLogin(string username)
	{
		if (string.IsNullOrEmpty(username)) return null;
		
		return await _dbContext.Players.FirstOrDefaultAsync(p => p.LoginName == username.MakeSafe());
	}
	
	public async Task GetPlayerStats(User player)
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
					{ Grade.XH, stat.XHCount },
					{ Grade.X, stat.XCount },
					{ Grade.SH, stat.SHCount },
					{ Grade.S, stat.SCount },
					{ Grade.A, stat.ACount }
				},
				TotalGekis = stat.TotalGekis,
				TotalKatus = stat.TotalKatus,
				Total300s = stat.Total300s,
				Total100s = stat.Total100s,
				Total50s = stat.Total50s
			};
		}
	}
	
	public async Task<StatsDto?> GetPlayerModeStats(int playerId, byte mode)
	{
		return await _dbContext.Stats.FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Mode == mode);
	}

	public async Task UpdatePlayerStats(User player, GameMode mode)
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
	
	public async Task GetPlayerRelationships(User player)
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
	
	public async Task UpdatePlayerPrivileges(User player, PlayerPrivileges playerPrivileges, bool remove)
	{
		if (remove)
			player.Privileges &= ~playerPrivileges;
		else
			player.Privileges |= playerPrivileges;
		
		await _dbContext.Players.Where(p => p.Id == player.Id)
		               .ExecuteUpdateAsync(p => 
			               p.SetProperty(u => u.Privileges, (int)player.Privileges));

		if (player.IsOnlineOnStable)
		{
			player.Enqueue(new ServerPackets()
				.BanchoPrivileges((int)player.ToBanchoPrivileges())
				.FinalizeAndGetContent());
		}
	}

	public async Task RecalculatePlayerTopScores(User player, GameMode mode)
	{
		var bestScores = await _dbContext.Scores
			.Where(s => s.PlayerId == player.Id
			            && s.Status == (int)SubmissionStatus.Best)
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

	public async Task UpdatePlayerRank(User player, GameMode mode)
	{
		var country = player.Geoloc.Country.Acronym;
		var stats = player.Stats[mode];

		switch (player.IsRestricted)
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
					Date = DateTime.UtcNow
				});
		}
	}
	
	public async Task CreatePlayer(string username, string email, string passwordHash, string country)
	{
		var playerDto = new PlayerDto
		{
			Username = username,
			LoginName = username.MakeSafe(),
			SafeName = username.MakeSafe(),
			Email = email,
			PasswordHash = passwordHash,
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
						Date = DateTime.UtcNow
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

	public async Task<bool> DeletePlayer(PlayerDto player, bool deleteScores, bool force)
	{
		var playerId = player.Id;
		
		var online = _players.GetPlayer(playerId);
		if (online != null)
		{
			if (!force) return false;
			
			_playerCoordinator.LogoutPlayer(online);
		}

		var batch = _redis.CreateBatch();

		for (byte i = 0; i < 8; i++)
		{
			var mode = i == 7 ? (byte)(i + 1) : i;

			await batch.SortedSetRemoveAsync($"bancho:leaderboard:{mode}", playerId);
			await batch.SortedSetRemoveAsync($"bancho:leaderboard:{mode}:{player.Country}", playerId);
		}

		batch.Execute();
		
		await _dbContext.Relationships.Where(r => r.PlayerId == playerId || r.TargetId == playerId).ExecuteDeleteAsync();
		await _dbContext.Stats.Where(s => s.PlayerId == playerId).ExecuteDeleteAsync();
		await _dbContext.Messages.Where(m => m.ReceiverId == playerId).ExecuteDeleteAsync();
		//TODO achievements, comments, favorites, club data
		
		if (deleteScores)
		{
			await _dbContext.Scores.Where(s => s.PlayerId == playerId).ExecuteDeleteAsync();
			await _dbContext.Players.Where(p => p.Id == playerId).ExecuteDeleteAsync();
		}
		else
		{
			var id = Guid.NewGuid().ToString()[..8];
			player.Username = $"delUser_{id}";
			player.SafeName = player.Username.MakeSafe();
			player.LoginName = player.SafeName;

			player.Email = "email";
			player.PasswordHash = "1";
			player.AwayMessage = "";
			player.UserPageContent = "";
			player.ApiKey = "";
		}
		
		await _histories.DeletePlayerData(playerId);

		return true;
	}

	public async Task<bool> SilencePlayer(User player, TimeSpan duration, string reason)
	{
		var modified = await _dbContext.Players.Where(p => p.Id == player.Id)
			.ExecuteUpdateAsync(s => s.SetProperty(p => p.RemainingSilence, DateTime.UtcNow + duration));

		if (modified != 1) return false;
		
		player.Enqueue(new ServerPackets()
			.SilenceEnd((int) duration.TotalSeconds)
			.FinalizeAndGetContent());
		
		_players.EnqueueToPlayers(new ServerPackets()
			.UserSilenced(player.Id)
			.FinalizeAndGetContent());
		
		//TODO store in db

		if (player.InMatch)
			_multiplayer.LeavePlayer(player);

		return true;
	}
	
	public async Task<bool> UnsilencePlayer(User player, string reason)
	{
		var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
		if (entity == null) return false;
		
		entity.RemainingSilence = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync();
		
		player.Enqueue(new ServerPackets()
			.SilenceEnd(0)
			.FinalizeAndGetContent());

		return true;
	}

	public async Task<bool> RestrictPlayer(User player, string reason)
	{
		var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
		if (entity == null) return false;
		
		//TODO log reason to database
		
		entity.Privileges &= ~(int)PlayerPrivileges.Unrestricted;
		await _dbContext.SaveChangesAsync();

		for (byte i = 0; i < 8; i++)
		{
			var mode = i == 7 ? (byte)(i + 1) : i;

			await RemovePlayerGlobalRank(mode, player.Geoloc.Country.Acronym, player.Id);
		}

		_playerCoordinator.LogoutPlayer(player);

		return true;
	}

	public async Task<bool> UnrestrictPlayer(User player, string reason)
	{
		var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
		if (entity == null) return false;
		
		entity.Privileges |= (int)PlayerPrivileges.Unrestricted;
		await _dbContext.SaveChangesAsync();

		if (!player.IsOnlineOnStable)
			await GetPlayerStats(player);

		foreach (var stats in player.Stats)
			await InsertPlayerGlobalRank((byte)stats.Key, player.Geoloc.Country.Acronym, player.Id, stats.Value.PP);
		
		_playerCoordinator.LogoutPlayer(player);

		return true;
	}
	
	/// <summary>
	/// Returns a list of objects that contains: player id, play count and replay views of players in a given mode.
	/// </summary>
	public async Task<List<PlayerHistoryStats>> GetPlayersModeStatsRange(
		byte mode,
		int count,
		int skip = 0,
		bool reset = false)
	{
		if (count % 8 != 0 || skip % 8 != 0)
			Console.WriteLine($"[Players] Taking/Skip count should be a multiple of 8, count: {count}, skip: {skip}");
		
		return await _dbContext.Stats
			.Include(s => s.Player)
			.Where(s => s.Mode == mode && !s.Player.Inactive && (s.Player.Privileges & 1) == 1)
			.OrderBy(s => s.PlayerId)
			.Skip(skip)
			.Take(count)
			.Select(s => new PlayerHistoryStats(s.PlayerId, s.PlayCount, s.ReplayViews))
			.ToListAsync();
	}

	public async Task ResetPlayersStats(byte mode)
	{
		await _dbContext.Stats.Where(s => s.Mode == mode)
			.ExecuteUpdateAsync(st => st.SetProperty(s => s.PlayCount, 0)
				.SetProperty(s => s.ReplayViews, 0));
	}

	/// <summary>
	/// Returns the total count of players (by default without restricted).
	/// </summary>
	public async Task<int> TotalPlayerCount(bool countRestricted = false)
	{
		return countRestricted
			? await _dbContext.Players.CountAsync()
			: await _dbContext.Players
				.Where(p => (p.Privileges & 1) == 1)
				.CountAsync();
	}

	/// <summary>
	/// Returns a list of player IDs with expired supporter status and updates their privileges.
	/// </summary>
	/// <returns>List of player IDs with expired supporter</returns>
	public async Task<List<int>> GetPlayerIdsWithExpiredSupporter()
	{
		var query = _dbContext.Players
			.Where(p => p.RemainingSupporter < DateTime.UtcNow
			            && (p.Privileges & (int)PlayerPrivileges.Supporter) == (int)PlayerPrivileges.Supporter);
		
		// Saving IDs before update
		var playerIds = await query.Select(p => p.Id).ToListAsync();

		// Updating supporter status
		await query.ExecuteUpdateAsync(s => s.SetProperty(p => p.RemainingSupporter, DateTime.MinValue)
			.SetProperty(p => p.Privileges, e => e.Privileges & ~(int)PlayerPrivileges.Supporter));
		
		return playerIds;
	}

	public async Task<int> GetPlayerGlobalRank(GameMode mode, int playerId)
	{
		return (int)(await _redis.SortedSetRankAsync($"bancho:leaderboard:{(byte)mode}", playerId, Order.Descending))! + 1;
	}

	public async Task InsertPlayerGlobalRank(byte mode, string country, int playerId, int pp)
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