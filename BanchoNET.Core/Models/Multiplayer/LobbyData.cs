namespace BanchoNET.Core.Models.Multiplayer;

public readonly struct LobbyData(MultiplayerLobby lobby, bool sendPassword)
{
    public readonly MultiplayerLobby Lobby = lobby;
    public readonly bool SendPassword = sendPassword;
}