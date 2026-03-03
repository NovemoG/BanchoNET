using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public interface IPlayerService : IDisposable
{
    Player BanchoBot { get; }
    IEnumerable<Player> Players { get; }
    IEnumerable<Player> PlayersInLobby { get; }
    IEnumerable<Player> Restricted { get; }
    IEnumerable<Player> Bots { get; }
    
    bool InsertPlayer(
        Player player,
        bool isBot = false
    );
    bool RemovePlayer(Player player);
    
    Player? GetPlayer(int id);
    Player? GetPlayer(string? username);
    Player? GetPlayer(Guid token);
    
    bool JoinLobby(Player player);
    bool LeaveLobby(Player player);
    
    public void SendMessageTo(
        Player player,
        string message,
        Player from,
        Channel? toChannel = null
    );
    public void SendBotMessageTo(
        Player player,
        string message,
        string toChannel = ""
    );
    
    void EnqueueToPlayers(byte[] data);
}