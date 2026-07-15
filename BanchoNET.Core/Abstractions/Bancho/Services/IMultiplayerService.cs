using BanchoNET.Core.Models.Stable.Multiplayer;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public interface IMultiplayerService
{
    IEnumerable<MultiplayerMatch> Matches { get; }
    
    ushort InsertLobby(MultiplayerMatch match);
    bool RemoveLobby(MultiplayerMatch match);
    MultiplayerMatch? GetMatch(ushort id);
}