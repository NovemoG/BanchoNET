using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Abstractions.Repositories;

public interface IPlayersRepository
{
    //TODO maybe split this because it feels too big
    
    Task<bool> EmailTaken(string email);
    Task<bool> UsernameTaken(string username);
    Task<bool> PlayerExists(string username);
    Task<bool> ChangeUsername(string oldUsername, string newUsername);
    
    Task AddFriend(Player player, int targetId);
    Task RemoveFriend(Player player, int targetId);
    
    Task<Player?> GetPlayerFromLogin(string username, string passwordMD5);
    Task<Player?> GetPlayerOrOffline(string username);
    Task<Player?> GetPlayerOrOffline(int playerId);
    Task<PlayerDto?> GetPlayerInfoFromLogin(string username);
    Task<PlayerDto?> GetPlayerInfo(int playerId);
    Task<PlayerDto?> GetPlayerInfo(string username);
    
    Task UpdateLatestActivity(Player player);
    Task UpdateLatestActivity(int playerId);
    Task UpdatePlayerCountry(Player player, string country);

    Task GetPlayerStats(Player player);
    Task<StatsDto?> GetPlayerModeStats(int playerId, byte mode);
    Task<List<Tuple<int, int, int>>> GetPlayersModeStatsRange(byte mode, int count, int skip = 0, bool reset = false);
    Task UpdatePlayerStats(Player player, GameMode mode);
    Task ResetPlayersStats(byte mode);
    
    Task GetPlayerRelationships(Player player);
    Task ModifyPlayerPrivileges(Player player, Privileges privileges, bool remove);

    Task RecalculatePlayerTopScores(Player player, GameMode mode);
    Task UpdatePlayerRank(Player player, GameMode mode);
    Task<int> GetPlayerGlobalRank(GameMode mode, int playerId);
    Task InsertPlayerGlobalRank(byte mode, string country, int playerId, ushort pp);
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