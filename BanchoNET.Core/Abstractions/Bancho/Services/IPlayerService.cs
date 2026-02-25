using BanchoNET.Core.Models.Users;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public interface IPlayerService : IDisposable
{
    User BanchoBot { get; }
    IEnumerable<User> Players { get; }
    IEnumerable<User> PlayersInLobby { get; }
    IEnumerable<User> Restricted { get; }
    IEnumerable<User> Bots { get; }
    
    bool InsertPlayer(
        User player,
        bool isBot = false
    );
    bool RemovePlayer(User player);
    
    User? GetPlayer(int id);
    User? GetPlayer(string? username);
    User? GetPlayer(Guid token);
    
    bool JoinLobby(User player);
    bool LeaveLobby(User player);
    
    void EnqueueToPlayers(byte[] data);
}