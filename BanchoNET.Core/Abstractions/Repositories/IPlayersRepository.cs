using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IPlayersRepository
{
    Task<bool> EmailTaken(string email);
    Task<bool> UsernameTaken(string username);
    Task<bool> PlayerExists(int userId);
    Task<bool> PlayerExists(string username);
    Task<bool> ChangeUsername(string oldUsername, string newUsername);
    Task<List<string>> GetPlayerNames(List<int> ids);
    Task<List<LookupApiPlayer>> GetPlayers(int[] ids);
    
    Task AddFriend(Player player, int targetId);
    Task<bool> AddRelation(int playerId, int targetId, byte relation);
    Task RemoveFriend(Player player, int targetId);
    Task RemoveRelation(int playerId, int targetId, byte relation);
    
    Task<PlayerDto?> GetPlayer(int playerId);
    Task<Player?> GetPlayerFromLogin(string username, string passwordMD5);
    Task<Player?> GetPlayerOrOffline(string username);
    Task<Player?> GetPlayerOrOffline(int playerId);
    Task<PlayerDto?> GetPlayerInfoFromLogin(string username);
    Task<PlayerDto?> GetPlayerInfo(int playerId);
    Task<PlayerDto?> GetPlayerInfoWithCustomization(int playerId);
    Task<PlayerDto?> GetPlayerInfo(string username);
    Task<MeResponse?> GetFullPlayerInfo(int playerId);
    Task<T?> GetFullPlayerInfo<T>(int playerId) where T : MeResponse, new();
    Task<T?> GetPlayerInfoForMode<T>(int playerId, GameMode? mode = null) where T : ApiPlayer, new();
    Task<List<BasicApiPlayer>> GetPlayersFromQuery(string query);

    /// <summary>
    /// Batched presence across both session kinds (stable in process, lazer in redis).
    /// Prefer this over <see cref="ILazerPlayerService.FilterOnline"/> in api paths, which only
    /// sees lazer clients.
    /// </summary>
    Task<HashSet<int>> FilterOnline(IReadOnlyCollection<int> playerIds);

    Task<HashSet<int>> FilterHidden(IReadOnlyCollection<int> playerIds);
    
    Task UpdateLatestActivity(Player player, bool updateInactivity = false);
    Task UpdateLatestActivity(int playerId, bool updateInactivity = false);
    Task UpdatePlayerCountry(Player player, string country);
    Task UpdatePlayerPmSetting(Player player, bool pmFriendsOnly);
    Task UpdatePlayerPassword(int playerId, string passwordHash);
    Task UpdatePlayerEmail(int playerId, string email);

    Task GetPlayerStats(Player player);
    Task<StatsDto?> GetPlayerModeStats(int playerId, byte mode);
    Task UpdatePlayerStats(Player player, GameMode mode, StatsDto stats);
    Task UpdatePlayerStats(StatsDto stats, ApiScore score);
    Task IncreasePlayerPlayTime(int playerId, int mode, int timeElapsed);
    Task IncreasePlayerReplaysViewed(int playerId, byte mode, long scoreId);
    
    Task<int> GetFriendsCount(int playerId);

    /// <summary>
    /// Stats for a set of players, each in their own preferred mode.
    /// </summary>
    Task<Dictionary<int, Statistics>> GetPlayersStatistics(int[] playerIds);
    Task<List<RelationshipReadDto>> GetPlayerBlocks(int playerId);
    Task<List<RelationshipReadDto>> GetPlayerFriends(int playerId);
    Task FetchPlayerRelationships(Player player);
    Task UpdatePlayerPrivileges(Player player, PlayerPrivileges playerPrivileges, bool remove);
    
    Task RecalculatePlayerTopScores(int playerId, StatsDto stats, GameMode mode);
    Task UpdatePlayerRank(int playerId,
        bool isRestricted,
        string country,
        StatsDto stats,
        GameMode mode
    );
    Task<int> GetPlayerGlobalRank(GameMode mode, int playerId);
    Task<int> GetPlayerCountryRank(GameMode mode, string country, int playerId);
    Task InsertPlayerGlobalRank(byte mode, string country, int playerId, int pp);
    Task RemovePlayerGlobalRank(byte mode, string country, int playerId);
    Task<List<PlayerRankingDto>> GetRanking(
        byte mode = 0,
        int page = 1,
        string country = "",
        bool filterByScore = false,
        int[]? restrictToPlayerIds = null
    );

    /// <summary>
    /// Per country totals aggregated from the same Stats rows the global ranking pages through.
    /// </summary>
    Task<(List<CountryRankingDto> Ranking, int Total)> GetCountryRanking(byte mode = 0, int page = 1);

    /// <summary>
    /// Distinct country codes that have ranked players, for the country selector.
    /// </summary>
    Task<List<string>> GetRankedCountries(byte mode = 0);

    Task CreatePlayer(string username, string email, string passwordHash, string country);
    Task<bool> DeletePlayer(PlayerDto player, bool deleteScores, bool force);
    Task<bool> SilencePlayer(Player player, TimeSpan duration, string reason);
    Task<bool> UnsilencePlayer(Player player, string reason);
    Task<bool> RestrictPlayer(Player player, string reason);
    Task<bool> UnrestrictPlayer(Player player, string reason);

    Task<int> TotalPlayerCount(bool countRestricted = false, string? country = null);
    Task<List<int>> GetPlayerIdsWithExpiredSupporter();
}