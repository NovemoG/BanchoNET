namespace BanchoNET.Core.Models.Stable.Multiplayer;

public readonly struct LobbyData(MultiplayerMatch match, bool sendPassword)
{
    public readonly MultiplayerMatch Match = match;
    public readonly bool SendPassword = sendPassword;
}