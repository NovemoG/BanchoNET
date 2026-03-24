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
    Task RemoveFriend(Player player, int targetId);
    
    Task<Player?> GetPlayerFromLogin(string username, string passwordMD5);
    Task<Player?> GetPlayerOrOffline(string username);
    Task<Player?> GetPlayerOrOffline(int playerId);
    Task<PlayerDto?> GetPlayerInfoFromLogin(string username);
    Task<PlayerDto?> GetPlayerInfo(int playerId);
    Task<PlayerDto?> GetPlayerInfo(string username);
    Task<MeResponse?> GetFullPlayerInfo(int playerId);
    Task<T?> GetPlayerInfoForMode<T>(int playerId, GameMode mode = GameMode.RelaxStd) where T : ApiPlayer, new();
    
    Task UpdateLatestActivity(Player player);
    Task UpdateLatestActivity(int playerId);
    Task UpdatePlayerCountry(Player player, string country);
    Task UpdatePlayerPmSetting(Player player, bool pmFriendsOnly);

    Task GetPlayerStats(Player player);
    Task<StatsDto?> GetPlayerModeStats(int playerId, byte mode);
    Task<List<PlayerHistoryStats>> GetPlayersModeStatsRange(byte mode, int count, int skip = 0, bool reset = false);
    Task UpdatePlayerStats(Player player, GameMode mode);
    Task UpdatePlayerStats(StatsDto stats, ApiScore score);
    Task ResetPlayersStats(byte mode);
    
    Task<List<RelationshipDto>> GetPlayerBlocks(int playerId);
    Task<List<RelationshipDto>> GetPlayerFriends(int playerId);
    Task FetchPlayerRelationships(Player player);
    Task UpdatePlayerPrivileges(Player player, PlayerPrivileges playerPrivileges, bool remove);

    Task RecalculatePlayerTopScores(Player player, GameMode mode);
    Task RecalculatePlayerTopScores(int playerId, StatsDto stats, GameMode mode);
    Task UpdatePlayerRank(Player player, GameMode mode);
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

    Task CreatePlayer(string username, string email, string passwordHash, string country);
    Task<bool> DeletePlayer(PlayerDto player, bool deleteScores, bool force);
    Task<bool> SilencePlayer(Player player, TimeSpan duration, string reason);
    Task<bool> UnsilencePlayer(Player player, string reason);
    Task<bool> RestrictPlayer(Player player, string reason);
    Task<bool> UnrestrictPlayer(Player player, string reason);

    Task<int> TotalPlayerCount(bool countRestricted = false);
    Task<List<int>> GetPlayerIdsWithExpiredSupporter();
}