using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IPlayersRepository
{
    Task<bool> EmailTaken(string email);
    Task<bool> UsernameTaken(string username);
    Task<bool> PlayerExists(string username);
    Task<bool> ChangeUsername(string oldUsername, string newUsername);
    Task<List<string>> GetPlayerNames(List<int> ids);
    
    Task AddFriend(User player, int targetId);
    Task RemoveFriend(User player, int targetId);
    
    Task<User?> GetPlayerFromLogin(string username, string passwordMD5);
    Task<User?> GetPlayerOrOffline(string username);
    Task<User?> GetPlayerOrOffline(int playerId);
    Task<PlayerDto?> GetPlayerInfoFromLogin(string username);
    Task<PlayerDto?> GetPlayerInfo(int playerId);
    Task<PlayerDto?> GetPlayerInfo(string username);
    
    Task UpdateLatestActivity(User player);
    Task UpdateLatestActivity(int playerId);
    Task UpdatePlayerCountry(User player, string country);

    Task GetPlayerStats(User player);
    Task<StatsDto?> GetPlayerModeStats(int playerId, byte mode);
    Task<List<PlayerHistoryStats>> GetPlayersModeStatsRange(byte mode, int count, int skip = 0, bool reset = false);
    Task UpdatePlayerStats(User player, GameMode mode);
    Task ResetPlayersStats(byte mode);
    
    Task GetPlayerRelationships(User player);
    Task UpdatePlayerPrivileges(User player, PlayerPrivileges playerPrivileges, bool remove);

    Task RecalculatePlayerTopScores(User player, GameMode mode);
    Task UpdatePlayerRank(User player, GameMode mode);
    Task<int> GetPlayerGlobalRank(GameMode mode, int playerId);
    Task InsertPlayerGlobalRank(byte mode, string country, int playerId, int pp);
    Task RemovePlayerGlobalRank(byte mode, string country, int playerId);

    Task CreatePlayer(string username, string email, string passwordHash, string country);
    Task<bool> DeletePlayer(PlayerDto player, bool deleteScores, bool force);
    Task<bool> SilencePlayer(User player, TimeSpan duration, string reason);
    Task<bool> UnsilencePlayer(User player, string reason);
    Task<bool> RestrictPlayer(User player, string reason);
    Task<bool> UnrestrictPlayer(User player, string reason);

    Task<int> TotalPlayerCount(bool countRestricted = false);
    Task<List<int>> GetPlayerIdsWithExpiredSupporter();
}