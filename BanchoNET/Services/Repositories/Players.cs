using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Statistics = BanchoNET.Core.Models.Api.Player.Statistics;

namespace BanchoNET.Services.Repositories;

public class PlayersRepository : IPlayersRepository
{
	private readonly BanchoDbContext _dbContext;
	private readonly IPlayerService _players;
	private readonly IPlayerCoordinator _playerCoordinator;
	private readonly IMultiplayerCoordinator _multiplayer;
	private readonly IDatabase _redis;
	private readonly IPlayerHistoryRepository _playerHistories;
	private readonly IPasswordService _passwords;
	private readonly ILazerPlayerService _lazerPlayers;

	public PlayersRepository(
		BanchoDbContext dbContext,
		IPlayerService players,
		IPlayerCoordinator playerCoordinator,
		IMultiplayerCoordinator multiplayer,
		IConnectionMultiplexer redis,
		IPlayerHistoryRepository playerHistories,
		IPasswordService passwords,
		ILazerPlayerService lazerPlayers
	) {
		_players = players;
		_playerCoordinator = playerCoordinator;
		_multiplayer = multiplayer;
		_dbContext = dbContext;
		_redis = redis.GetDatabase();
		_playerHistories = playerHistories;
		_passwords = passwords;
		_lazerPlayers = lazerPlayers;
	}

	private async Task<bool> IsPlayerOnline(
		int playerId
	) {
		if (_players.GetPlayer(playerId) != null) return true;

		return (await _lazerPlayers.FilterOnline([playerId])).Count != 0;
	}

	public async Task<HashSet<int>> FilterHidden(
		IReadOnlyCollection<int> playerIds
	) {
		return playerIds.Count == 0 ? [] : await _lazerPlayers.FilterHidden(playerIds);
	}

	public async Task<HashSet<int>> FilterOnline(
		IReadOnlyCollection<int> playerIds
	) {
		if (playerIds.Count == 0) return [];

		var online = await _lazerPlayers.FilterOnline(playerIds);

		foreach (var playerId in playerIds)
		{
			if (_players.GetPlayer(playerId) != null)
				online.Add(playerId);
		}

		return online;
	}
	
	public async Task<bool> EmailTaken(string email)
	{
		return await _dbContext.Players.AnyAsync(p => p.Email == email);
	}
	
	public async Task<bool> UsernameTaken(string username)
	{
		return await _dbContext.Players.AnyAsync(p => p.SafeName == username.MakeSafe());
	}

	public async Task<bool> PlayerExists(int userId)
	{
		return await _dbContext.Players.AnyAsync(p => p.Id == userId);
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

	public async Task<List<LookupApiPlayer>> GetPlayers(
		int[] ids
	) {
		var players = await _dbContext.Players
			.Where(p => ids.AsEnumerable().Contains(p.Id))
			.Select(p => new LookupApiPlayer
			{
				CountryCode = p.Country.ToUpper(),
				Country = p.Country.ParseCountry(),
				Id = p.Id,
				IsActive = !p.Inactive,
				IsDeleted = p.Deleted,
				IsSupporter = p.IsSupporter,
				LastVisit = p.HideOnlineActivity ? null : p.LastActivityTime,
				PmFriendsOnly = p.PmFriendsOnly,
				Username = p.Username,
				PreferredMode = (byte)p.PreferredMode,
				AvatarUrl = ProfileAssetUrls.Avatar(p.Id, p.AvatarUpdatedAt),
				Cover = ProfileAssetUrls.Cover(p.Id, p.CoverPresetId, p.CoverFile, p.CoverUpdatedAt),
			}).ToListAsync();

		foreach (var player in players)
		{
			player.GlobalRank = new GlobalRank
			{
				Rank = await GetPlayerGlobalRank((GameMode)player.PreferredMode, player.Id),
				RulesetId = player.PreferredMode
			};
		}

		await AssignPresence(players);

		return players;
	}

	public async Task AddFriend(Player player, int targetId)
	{
		if (player.Friends.Contains(targetId))
			return;
		
		player.Friends.Add(targetId);

		await AddRelation(player.Id, targetId, (byte)Relations.Friend);
	}

	public async Task<bool> AddRelation(
		int playerId,
		int targetId,
		byte relation
	) {
		var existing = await _dbContext.Relationships
			.AsNoTracking()
			.FirstOrDefaultAsync(r =>
				r.PlayerId == playerId
				&& r.TargetId == targetId
				&& r.Relation == relation
			);

		if (existing == null)
		{
			existing = new RelationshipDto
			{
				PlayerId = playerId,
				TargetId = targetId,
				Relation = relation
			};

			_dbContext.Relationships.Add(existing);
			await _dbContext.SaveChangesAsync();
		}

		if (relation == (byte)Relations.Block) return false;

		return await _dbContext.Relationships.AnyAsync(r =>
			r.PlayerId == targetId
			&& r.TargetId == playerId
			&& r.Relation == relation
		);
	}

	public async Task RemoveFriend(Player player, int targetId)
	{
		if (!player.Friends.Contains(targetId))
			return;
		
		player.Friends.Remove(targetId);

		await RemoveRelation(player.Id, targetId, (byte)Relations.Friend);
	}

	public async Task RemoveRelation(
		int playerId,
		int targetId,
		byte relation
	) {
		await _dbContext.Relationships
			.Where(r => r.PlayerId == playerId && r.TargetId == targetId && r.Relation == relation)
			.ExecuteDeleteAsync();
	}

	public async Task<PlayerDto?> GetPlayer(
		int playerId
	) {
		return await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId);
	}

	public async Task<Player?> GetPlayerFromLogin(string username, string passwordMD5)
	{
		var player = await GetPlayerOrOffline(username);
		if (player == null) return null;

		return _passwords.Verify(passwordMD5, player.PasswordHash) ? player : null;
	}
	
	public async Task<Player?> GetPlayerOrOffline(string username)
	{
		var sessionPlayer = _players.GetPlayer(username);
		if (sessionPlayer != null) return sessionPlayer;
		
		var dbPlayer = await _dbContext.Players.FirstOrDefaultAsync(p => p.SafeName == username.MakeSafe());

		return dbPlayer == null ? null : new Player(dbPlayer);
	}
	
	public async Task<Player?> GetPlayerOrOffline(int playerId)
	{
		var sessionPlayer = _players.GetPlayer(playerId);
		if (sessionPlayer != null) return sessionPlayer;
		
		var dbPlayer = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId);

		return dbPlayer == null ? null : new Player(dbPlayer);
	}

	public async Task<MeResponse?> GetFullPlayerInfo(
		int playerId
	) => await GetFullPlayerInfo<MeResponse>(playerId);

	public async Task<T?> GetFullPlayerInfo<T>(
		int playerId
	) where T : MeResponse, new() {
		var player = await GetPlayerInfoForMode<T>(playerId);
		if (player == null) return null;

		var vanillaModes = new[] {
			GameMode.VanillaStd,
			GameMode.VanillaTaiko,
			GameMode.VanillaMania,
			GameMode.VanillaCatch
		};

		var preferred = EnumExtensions.ToModeMap[player.Playmode];
		var otherModes = vanillaModes.Where(m => m != preferred).ToArray();
		
		AssignStatistics(player.StatisticsRulesets, preferred, player.Statistics);
		
		foreach (var mode in otherModes)
		{
			AssignStatistics(
				player.StatisticsRulesets,
				mode,
				await FetchModeStatistics(playerId, mode, player.CountryCode)
			);
		}
		
		return player;
	}

	private static void AssignStatistics(
		StatisticsRulesets target,
		GameMode mode,
		Statistics? stats
	) {
		switch (mode)
		{
			case GameMode.VanillaStd: target.Osu = stats; break;
			case GameMode.VanillaTaiko: target.Taiko = stats; break;
			case GameMode.VanillaCatch: target.Fruits = stats; break;
			case GameMode.VanillaMania: target.Mania = stats; break;
		}
	}

	private const int RankHistoryDays = 90;

	public async Task<T?> GetPlayerInfoForMode<T>(
		int playerId,
		GameMode? mode = null
	) where T : ApiPlayer, new() {
		var userInfo = await GetPlayerInfoWithCustomization(playerId);
		if (userInfo == null) return null;

		var playerMode = mode ?? userInfo.PreferredMode;
		var country = userInfo.Country.ParseCountry();

		var modeStats = await GetPlayerModeStats(playerId, (byte)playerMode);
		var stats = await FetchModeStatistics(playerId, playerMode, userInfo.Country);

		var history = await BuildHistory(playerId, (byte)playerMode, userInfo, modeStats);

		var hidden = userInfo.HideOnlineActivity || (await FilterHidden([playerId])).Count != 0;
		var online = !hidden && await IsPlayerOnline(playerId);
		var cover = ProfileAssetUrls.Cover(
			playerId,
			userInfo.CoverPresetId,
			userInfo.CoverFile,
			userInfo.CoverUpdatedAt);

		var player = new T
		{
			CountryCode = country.Code,
			Id = playerId,
			IsActive = !userInfo.Inactive,
			IsBot = false,
			IsDeleted = userInfo.Deleted,
			IsOnline = online,
			IsSupporter = userInfo.RemainingSupporter > DateTime.UtcNow,
			SupportLevel = userInfo.SupporterLevel,
			LastVisit = hidden ? null : userInfo.LastActivityTime,
			PmFriendsOnly = userInfo.PmFriendsOnly,
			Username = userInfo.Username,
			HasSupported = userInfo.HasSupported,
			Title = userInfo.Title,
			JoinDate = userInfo.CreationTime,
			Playmode = EnumExtensions.FromModeMap[userInfo.PreferredMode],
			Country = country,
			IsRestricted = (userInfo.Privileges & 1) == 0,
			FollowerCount = await GetFriendsCount(playerId),
			MonthlyPlaycounts = history.MonthlyPlaycounts,
			ReplaysWatchedCounts = history.ReplaysWatchedCounts,
			AvatarUrl = ProfileAssetUrls.Avatar(playerId, userInfo.AvatarUpdatedAt),
			Cover = cover,
			CoverUrl = cover.Url,
			Location = userInfo.UserFrom,
			Interests = userInfo.UserInterests,
			Occupation = userInfo.UserOcc,
			Twitter = userInfo.UserTwitter,
			Discord = userInfo.UserDiscord,
			Website = userInfo.UserWebsite,
			Playstyle = userInfo.PlayStyle.ToNames(),
			Page = new Page { Raw = userInfo.UserPageContent ?? "" }, //TODO render html from BBCode
			ProfileHue = userInfo.ProfileCustomization?.ProfileHue ?? 0,
			RankHighest = modeStats is { PeakRank: > 0, PeakRankDate: not null }
				? new RankHighest
				{
					Rank = modeStats.PeakRank,
					UpdatedAt = modeStats.PeakRankDate.Value
				}
				: null,
			ScoresBestCount = userInfo.TopPlaysCount,
			ScoresPinnedCount = 0, //TODO
			Statistics = stats,
			DailyChallengeUserStats = {
				UserId = playerId
			}
			//TODO team
			//TODO achievements
		};
		
		//TODO
		player.MatchmakingStats[0].UserId = playerId;
		player.MatchmakingStats[0].Rank = stats.GlobalRank ?? 0;  //TODO
		
		player.RankHistory.Data = history.RankHistory;
		player.RankHistory.Mode = EnumExtensions.FromModeMap[playerMode.AsVanilla()];

		return player;
	}

	private readonly record struct PlayerHistorySections(
		int[] RankHistory,
		MonthlyPlaycounts[] MonthlyPlaycounts,
		ReplaysWatchedCounts[] ReplaysWatchedCounts
	);

	private async Task<PlayerHistorySections> BuildHistory(
		int playerId,
		byte mode,
		PlayerDto userInfo,
		StatsDto? modeStats
	) {
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var joined = DateOnly.FromDateTime(userInfo.CreationTime);

		var rankSamples = await _playerHistories.GetSeries(
			playerId,
			mode,
			[HistoryMetric.GlobalRank],
			HistoryGranularity.Daily,
			today.AddDays(-(RankHistoryDays - 1))
		);

		var monthlySamples = (await _playerHistories.GetSeries(
			playerId,
			mode,
			[HistoryMetric.PlayCount, HistoryMetric.ReplayViews],
			HistoryGranularity.Monthly
		)).ToLookup(s => s.Metric);

		var playCounts = PlayerHistorySeries.ToMonthlySeries(
			monthlySamples[HistoryMetric.PlayCount].ToList(),
			joined,
			today,
			modeStats?.PlayCount ?? 0
		);

		var replayViews = PlayerHistorySeries.ToMonthlySeries(
			monthlySamples[HistoryMetric.ReplayViews].ToList(),
			joined,
			today,
			modeStats?.ReplayViews ?? 0,
			trimLeadingEmptyMonths: true
		);

		return new PlayerHistorySections(
			PlayerHistorySeries.ToDailyWindow(rankSamples, today, RankHistoryDays),
			playCounts
				.Select(m => new MonthlyPlaycounts
				{
					StartDate = m.Month.ToDateTime(TimeOnly.MinValue),
					Count = m.Count
				}).ToArray(),
			replayViews
				.Select(m => new ReplaysWatchedCounts
				{
					StartDate = m.Month.ToDateTime(TimeOnly.MinValue),
					Count = m.Count
				}).ToArray()
		);
	}

	public async Task<List<BasicApiPlayer>> GetPlayersFromQuery(
		string query
	) {
		var players = await _dbContext.Players
			.Where(p => (p.Privileges & 1) == 1
			            && EF.Functions.ILike(p.Username, $"%{query.Replace("_", @"\_")}%"))
			.Select(p => new BasicApiPlayer(p))
			.ToListAsync();

		await AssignPresence(players);

		return players;
	}

	private async Task AssignPresence<T>(
		List<T> players
	) where T : BasicApiPlayer {
		if (players.Count == 0) return;

		var ids = players.Select(p => p.Id).ToArray();
		var online = await FilterOnline(ids);
		var hidden = await FilterHidden(ids);

		foreach (var player in players)
		{
			if (hidden.Contains(player.Id))
				player.LastVisit = null;

			player.IsOnline = player.LastVisit != null && online.Contains(player.Id);
		}
	}
	
	public async Task<Dictionary<int, Statistics>> GetPlayersStatistics(
		int[] playerIds
	) {
		if (playerIds.Length == 0) return [];

		var players = await _dbContext.Players
			.AsNoTracking()
			.Where(p => playerIds.Contains(p.Id))
			.Select(p => new { p.Id, p.PreferredMode, p.Country })
			.ToListAsync();

		var result = new Dictionary<int, Statistics>(players.Count);

		foreach (var player in players)
		{
			if (await GetPlayerModeStats(player.Id, (byte)player.PreferredMode) == null) continue;

			result[player.Id] = await FetchModeStatistics(player.Id, player.PreferredMode, player.Country);
		}

		return result;
	}

	private async Task<Statistics> FetchModeStatistics(
		int playerId,
		GameMode mode,
		string country
	) {
		var dbStats = (await GetPlayerModeStats(playerId, (byte)mode))!;
		var rank = await GetPlayerGlobalRank(mode, playerId);
		var countryRank = await GetPlayerCountryRank(mode, country, playerId);
		
		var stats = new Statistics {
			Count100 = dbStats.Total100s,
			Count300 = dbStats.Total300s,
			Count50 = dbStats.Total50s,
			TotalHits = mode switch
			{
				GameMode.VanillaStd => dbStats.TotalStdHits,
				GameMode.VanillaTaiko => dbStats.TotalTaikoHits,
				GameMode.VanillaMania => dbStats.TotalManiaHits,
				GameMode.VanillaCatch => dbStats.TotalCatchHits,
				_ => 0
			},
			GlobalRank = rank,
			GlobalRankPercent = 0.0000001d, //TODO
			Pp = dbStats.PP,
			RankedScore = dbStats.RankedScore,
			HitAccuracy = dbStats.Accuracy,
			Accuracy = dbStats.Accuracy / 100f,
			PlayCount = dbStats.PlayCount,
			PlayTime = dbStats.PlayTime,
			TotalScore = dbStats.TotalScore,
			MaximumCombo = dbStats.MaxCombo,
			ReplaysWatchedByOthers = dbStats.ReplayViews,
			IsRanked = dbStats.IsRanked,
			GradeCounts = new GradeCounts {
				Ss = dbStats.XCount,
				Ssh = dbStats.XHCount,
				S = dbStats.SCount,
				Sh = dbStats.SHCount,
				A = dbStats.ACount
			},
			CountryRank = countryRank,
			Rank = new Rank{ Country = countryRank }
			//TODO if mania add variant for 4k and 7k
		};

		return stats;
	}

	public async Task UpdateLatestActivity(Player player, bool updateInactivity = false)
	{
		player.LastActivityTime = DateTime.UtcNow;
		
		await UpdateLatestActivity(player.Id, updateInactivity);
	}
	
	public async Task UpdateLatestActivity(int playerId, bool updateInactivity = false)
	{
		await _dbContext.Players
			.Where(p => p.Id == playerId)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(u => u.LastActivityTime, DateTime.UtcNow)
				 .SetProperty(u => u.Inactive, u => !updateInactivity && u.Inactive));
	}
	
	public async Task UpdatePlayerCountry(Player player, string country)
	{
		await _dbContext.Players
			.Where(p => p.Id == player.Id)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(u => u.Country, country));
	}

	public async Task UpdatePlayerPmSetting(
		Player player,
		bool pmFriendsOnly
	) {
		await _dbContext.Players
			.Where(p => p.Id == player.Id)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(u => u.PmFriendsOnly, pmFriendsOnly));
	}

	public async Task UpdatePlayerPassword(
		int playerId,
		string passwordHash
	) {
		await _dbContext.Players
			.Where(p => p.Id == playerId)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(u => u.PasswordHash, passwordHash));
	}

	public async Task UpdatePlayerEmail(
		int playerId,
		string email
	) {
		await _dbContext.Players
			.Where(p => p.Id == playerId)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(u => u.Email, email));
	}

	public async Task<PlayerDto?> GetPlayerInfo(int playerId)
	{
		if (playerId < 1) return null;

		return await _dbContext.Players.FindAsync(playerId);
	}

	public async Task<PlayerDto?> GetPlayerInfoWithCustomization(
		int playerId
	) {
		if (playerId < 1) return null;

		return await _dbContext.Players
			.AsNoTracking()
			.Include(p => p.ProfileCustomization)
			.FirstOrDefaultAsync(p => p.Id == playerId);
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
	
	public async Task GetPlayerStats(Player player)
	{
		var stats = await _dbContext.Stats
			.AsNoTracking()
			.Where(s => s.PlayerId == player.Id).ToListAsync();
		
		foreach (var stat in stats)
		{
			var mode = (GameMode)stat.Mode;
			
			player.Stats[mode] = stat.ToModeStats(await GetPlayerGlobalRank(mode, player.Id));
		}
	}
	
	public async Task<StatsDto?> GetPlayerModeStats(int playerId, byte mode)
	{
		return await _dbContext.Stats.FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Mode == mode);
	}

	public async Task UpdatePlayerStats(
		Player player,
		GameMode mode,
		StatsDto stats
	) {
		await _dbContext.SaveChangesAsync();

		player.Stats[mode] = stats.ToModeStats(await GetPlayerGlobalRank(mode, player.Id));
	}

	public async Task UpdatePlayerStats(
		StatsDto stats,
		ApiScore score
	) {
		stats.PlayCount += 1;
		stats.TotalScore += score.TotalScore;

		var statistics = score.Statistics;
		
		stats.Total300s += statistics.GetStatCount(HitResult.Great);
		stats.Total100s += statistics.GetStatCount(HitResult.Ok);
		stats.Total50s += statistics.GetStatCount(HitResult.Meh);

		if (((GameMode)score.RulesetId).AsVanilla() is GameMode.VanillaMania or GameMode.VanillaTaiko)
		{
			stats.TotalGekis += statistics.GetStatCount(HitResult.LargeTickHit);
			stats.TotalKatus += statistics.GetStatCount(HitResult.SliderTailHit);
		}
		
		await _dbContext.SaveChangesAsync();
	}

	public async Task IncreasePlayerPlayTime(
		int playerId,
		int mode,
		int timeElapsed
	) {
		await _dbContext.Stats.Where(s => s.PlayerId == playerId && s.Mode == mode)
			.ExecuteUpdateAsync(p => p.SetProperty(s => s.PlayTime, s => s.PlayTime + timeElapsed));
	}

	public async Task IncreasePlayerReplaysViewed(
		int playerId,
		byte mode,
		long scoreId
	) {
		var stats = await _dbContext.Stats.FindAsync(playerId, mode);
		if (stats is null)
			return;
		
		var replayWatches = await _dbContext.ReplayWatches
			.SingleOrDefaultAsync(rw => rw.ScoreId == scoreId && rw.PlayerId == playerId);

		if (replayWatches is null)
		{
			_dbContext.ReplayWatches.Add(new ReplayWatches
			{
				ScoreId = scoreId,
				PlayerId = playerId,
				Count = 1
			});
		}
		else replayWatches.Count += 1;
		
		stats.ReplayViews += 1;
		
		await _dbContext.SaveChangesAsync();
	}

	public async Task<int> GetFriendsCount(
		int playerId
	) {
		return await _dbContext.Relationships
			.AsNoTracking()
			.Where(r => r.TargetId == playerId && r.Relation == (byte)Relations.Friend)
			.CountAsync();
	}

	public async Task<List<RelationshipReadDto>> GetPlayerBlocks(
		int playerId
	) {
		return await _dbContext.Relationships
			.AsNoTracking()
			.Where(p => p.PlayerId == playerId && p.Relation == (byte)Relations.Block)
			.Include(p => p.Target)
			.Select(r => new RelationshipReadDto
			{
				PlayerId = r.PlayerId,
				TargetId = r.TargetId,
				Relation = r.Relation,
				Target = r.Target
			})
			.ToListAsync();
	}

	public async Task<List<RelationshipReadDto>> GetPlayerFriends(
		int playerId
	) {
		return await _dbContext.Relationships
			.AsNoTracking()
			.Where(p => p.PlayerId == playerId && p.Relation == (byte)Relations.Friend)
			.Include(p => p.Target)
			.Select(r => new RelationshipReadDto
			{
				PlayerId = r.PlayerId,
				TargetId = r.TargetId,
				Relation = r.Relation,
				Mutual = _dbContext.Relationships.Any(x =>
					x.PlayerId == r.TargetId
					&& x.TargetId == r.PlayerId
					&& x.Relation == r.Relation),
				Target = r.Target
			})
			.ToListAsync();
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
	
	public async Task UpdatePlayerPrivileges(Player player, PlayerPrivileges playerPrivileges, bool remove)
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
	
	public async Task RecalculatePlayerTopScores(
		int playerId,
		StatsDto stats,
		GameMode mode
	) {
		var bestScores = await _dbContext.Scores
			.Where(s => s.PlayerId == playerId
			            && s.Status == SubmissionStatus.Best
			            && s.Mode == mode
			            && s.Ranked)
			.OrderByDescending(s => s.PP)
			.Take(AppSettings.TopPlaysCount)
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
		
		stats.Accuracy = weightedAcc * accWeight / 100;
		stats.PP = (ushort)MathF.Round(weightedPp + bonusPp);
		await _dbContext.SaveChangesAsync();

		await _dbContext.Players.Where(p => p.Id == playerId)
			.ExecuteUpdateAsync(s => s.SetProperty(
				p => p.TopPlaysCount, Math.Min(bestScores.Count, AppSettings.TopPlaysCount)
			));
	}

	public async Task UpdatePlayerRank(
		int playerId,
		bool isRestricted,
		string country,
		StatsDto stats,
		GameMode mode
	) {
		switch (isRestricted)
		{
			case false:
				await InsertPlayerGlobalRank((byte)mode, country, playerId, stats.PP);
				break;
			case true:
				//TODO stats.Rank = 0;
				return;
		}

		var rank = await GetPlayerGlobalRank(mode, playerId);

		// 0 means never ranked
		if (stats.PeakRank != 0 && rank >= stats.PeakRank) return;

		stats.PeakRank = rank;
		stats.PeakRankDate = DateTime.UtcNow;
		
		await _dbContext.SaveChangesAsync();
	}

	public async Task<List<PlayerRankingDto>> GetRanking(
		byte mode = 0,
		int page = 1,
		string country = "",
		bool filterByScore = false,
		int[]? restrictToPlayerIds = null
	) {
		var ranking = await _dbContext.Stats
			.AsNoTracking()
			.Include(s => s.Player)
			.Where(s => s.Mode == mode
			            && s.IsRanked
			            && (string.IsNullOrEmpty(country) || s.Player.Country == country)
			            && (restrictToPlayerIds == null || restrictToPlayerIds.Contains(s.PlayerId))
			)
			.OrderByDescending(s => filterByScore ? s.TotalScore : s.PP)
			.Skip((page - 1) * 50)
			.Take(50)
			.Select(s => new PlayerRankingDto
			{
				Stats = s,
				Player = s.Player
			})
			.ToListAsync();

		await AssignRankChange(ranking, mode, page);

		return ranking;
	}

	public async Task<List<string>> GetRankedCountries(
		byte mode = 0
	) {
		return await _dbContext.Stats
			.AsNoTracking()
			.Where(s => s.Mode == mode && s.IsRanked)
			.Select(s => s.Player.Country)
			.Distinct()
			.ToListAsync();
	}

	public async Task<(List<CountryRankingDto> Ranking, int Total)> GetCountryRanking(
		byte mode = 0,
		int page = 1
	) {
		var query = _dbContext.Stats
			.AsNoTracking()
			.Where(s => s.Mode == mode && s.IsRanked)
			.GroupBy(s => s.Player.Country)
			.Select(g => new CountryRankingDto
			{
				Code = g.Key,
				ActiveUsers = g.Count(),
				PlayCount = g.Sum(s => (long)s.PlayCount),
				RankedScore = g.Sum(s => s.RankedScore),
				Performance = g.Sum(s => (long)s.PP)
			});

		var total = await query.CountAsync();
		var ranking = await query
			.OrderByDescending(c => c.Performance)
			.Skip((page - 1) * 50)
			.Take(50)
			.ToListAsync();

		return (ranking, total);
	}
	
	private async Task AssignRankChange(
		List<PlayerRankingDto> ranking,
		byte mode,
		int page
	) {
		if (ranking.Count == 0) return;

		var playerIds = ranking.Select(r => r.Stats.PlayerId).ToList();

		var previous = await _playerHistories.GetSamplesAsOf(
			playerIds,
			mode,
			HistoryMetric.GlobalRank,
			HistoryGranularity.Daily,
			DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30)
		);

		if (previous.Count == 0) return;
		
		var firstRank = (page - 1) * 50 + 1;

		for (var i = 0; i < ranking.Count; i++)
		{
			if (!previous.TryGetValue(ranking[i].Stats.PlayerId, out var previousRank) || previousRank <= 0)
				continue;

			ranking[i].RankChangeSince30Days = (int)previousRank - (firstRank + i);
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
		
		var player = _dbContext.Players.Add(playerDto);
		await _dbContext.SaveChangesAsync();

		var playerId = player.Entity.Id;
		
		var trackedModes = ModeExtensions.TrackedModes;
		var scoreDtos = new StatsDto[trackedModes.Length];

		for (var i = 0; i < trackedModes.Length; i++)
		{
			var mode = trackedModes[i];
			
			scoreDtos[i] = new StatsDto
			{
				PlayerId = playerId,
				Mode = mode
			};
			
			await _redis.SortedSetAddAsync($"bancho:leaderboard:{mode}", playerId, 0);
		}

		await _dbContext.Stats.AddRangeAsync(scoreDtos);
		
		_dbContext.PlayerProfileCustomizations.Add(new PlayerProfileCustomizationDto
		{
			PlayerId = playerId,
			UpdatedAt = DateTime.UtcNow
		});
		await _dbContext.SaveChangesAsync();
	}

	public async Task<bool> DeletePlayer(PlayerDto player, bool deleteScores, bool force)
	{
		var playerId = player.Id;
		
		var online = _players.GetPlayer(playerId);
		if (online != null)
		{
			if (!force) return false;

			await _playerCoordinator.LogoutPlayer(online);
		}

		var batch = _redis.CreateBatch();

		foreach (var mode in ModeExtensions.TrackedModes)
		{
			await batch.SortedSetRemoveAsync($"bancho:leaderboard:{mode}", playerId);
			await batch.SortedSetRemoveAsync($"bancho:leaderboard:{mode}:{player.Country}", playerId);
		}

		batch.Execute();
		
		await _dbContext.Relationships.Where(r => r.PlayerId == playerId || r.TargetId == playerId).ExecuteDeleteAsync();
		await _dbContext.Stats.Where(s => s.PlayerId == playerId).ExecuteDeleteAsync();
		await _dbContext.PlayerHistories.Where(h => h.PlayerId == playerId).ExecuteDeleteAsync();
		//TODO achievements, comments, favorites, club data
		
		Storage.DeleteProfileImages(playerId);

		if (deleteScores)
		{
			await _dbContext.Scores.Where(s => s.PlayerId == playerId).ExecuteDeleteAsync();
			await _dbContext.Players.Where(p => p.Id == playerId).ExecuteDeleteAsync();
		}
		else
		{
			var id = Guid.NewGuid().ToString()[..8];
			var oldPasswordHash = player.PasswordHash;

			player.Username = $"delUser_{id}";
			player.SafeName = player.Username.MakeSafe();
			player.LoginName = player.SafeName;

			player.Email = $"delUser_{id}@deleted.invalid";
			player.PasswordHash = "1";
			player.AwayMessage = "";
			player.UserPageContent = "";
			player.ApiKey = null;

			player.UserFrom = null;
			player.UserInterests = null;
			player.UserOcc = null;
			player.UserTwitter = null;
			player.UserDiscord = null;
			player.UserWebsite = null;
			player.UserSig = null;
			player.PlayStyle = Playstyle.None;

			player.AvatarExtension = null;
			player.AvatarUpdatedAt = null;
			player.CoverPresetId = null;
			player.CoverFile = null;
			player.CoverUpdatedAt = null;

			player.Deleted = true;
			player.Privileges = 0;

			_passwords.Invalidate(oldPasswordHash);

			await _dbContext.SaveChangesAsync();
		}

		return true;
	}

	public async Task<bool> SilencePlayer(Player player, TimeSpan duration, string reason)
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
			await _multiplayer.LeavePlayer(player);

		return true;
	}
	
	public async Task<bool> UnsilencePlayer(Player player, string reason)
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

	public async Task<bool> RestrictPlayer(Player player, string reason)
	{
		var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
		if (entity == null) return false;
		
		//TODO log reason to database
		
		entity.Privileges &= ~(int)PlayerPrivileges.Unrestricted;
		await _dbContext.SaveChangesAsync();

		foreach (var mode in ModeExtensions.TrackedModes)
			await RemovePlayerGlobalRank(mode, player.Geoloc.Country.Acronym, player.Id);
		
		await _dbContext.Stats.Where(s => s.PlayerId == player.Id)
			.ExecuteUpdateAsync(p => p.SetProperty(s => s.IsRanked, false));

		await _playerCoordinator.LogoutPlayer(player);

		return true;
	}

	public async Task<bool> UnrestrictPlayer(Player player, string reason)
	{
		var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
		if (entity == null) return false;
		
		entity.Privileges |= (int)PlayerPrivileges.Unrestricted;
		await _dbContext.SaveChangesAsync();

		if (!player.IsOnlineOnStable)
			await GetPlayerStats(player);

		foreach (var stats in player.Stats)
			await InsertPlayerGlobalRank((byte)stats.Key, player.Geoloc.Country.Acronym, player.Id, stats.Value.PP);
		
		await _dbContext.Stats.Where(s => s.PlayerId == player.Id)
			.ExecuteUpdateAsync(p => p.SetProperty(s => s.IsRanked, true));

		await _playerCoordinator.LogoutPlayer(player);

		return true;
	}
	
	/// <summary>
	/// Returns the total count of players (by default without restricted).
	/// </summary>
	public async Task<int> TotalPlayerCount(
		bool countRestricted = false,
		string? country = null
	) {
		return countRestricted
			? await _dbContext.Players.CountAsync()
			: await _dbContext.Players
				.Where(p => (p.Privileges & 1) == 1
				            && (string.IsNullOrEmpty(country) || p.Country == country))
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

	public async Task<int> GetPlayerGlobalRank(
		GameMode mode,
		int playerId
	) {
		var rank = await _redis.SortedSetRankAsync($"bancho:leaderboard:{(byte)mode}", playerId, Order.Descending);
		if (rank == null) return 0;
		return (int)rank + 1;
	}

	public async Task<int> GetPlayerCountryRank(
		GameMode mode,
		string country,
		int playerId
	) {
		var rank = await _redis.SortedSetRankAsync($"bancho:leaderboard:{(byte)mode}:{country.ToLower()}", playerId, Order.Descending);
		if (rank == null) return 0;
		return (int)rank + 1;
	}

	public async Task InsertPlayerGlobalRank(byte mode, string country, int playerId, int pp)
	{
		await _redis.SortedSetAddAsync($"bancho:leaderboard:{mode}", playerId, pp);
		await _redis.SortedSetAddAsync($"bancho:leaderboard:{mode}:{country.ToLower()}", playerId, pp);
	}

	public async Task RemovePlayerGlobalRank(byte mode, string country, int playerId)
	{
		await _redis.SortedSetRemoveAsync($"bancho:leaderboard:{mode}", playerId);
		await _redis.SortedSetRemoveAsync($"bancho:leaderboard:{mode}:{country.ToLower()}", playerId);
	}
}