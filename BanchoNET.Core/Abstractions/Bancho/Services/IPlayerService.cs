using BanchoNET.Core.Models.Channels;
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
    
    public void SendMessageTo(
        User player,
        string message,
        User from,
        Channel? toChannel = null
    );
    public void SendBotMessageTo(
        User player,
        string message,
        string toChannel = ""
    );
    
    void EnqueueToPlayers(byte[] data);
}