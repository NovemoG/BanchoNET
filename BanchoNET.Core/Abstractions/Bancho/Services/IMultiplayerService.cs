using BanchoNET.Core.Models.Multiplayer;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public interface IMultiplayerService
{
    ushort GetFreeMatchId { get; }
    IEnumerable<MultiplayerMatch> Matches { get; }
    
    bool InsertLobby(MultiplayerMatch match);
    bool RemoveLobby(MultiplayerMatch match);
    MultiplayerMatch? GetMatch(ushort id);
}