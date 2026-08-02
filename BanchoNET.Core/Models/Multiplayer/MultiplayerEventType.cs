namespace BanchoNET.Core.Models.Multiplayer;

/// <summary>
/// An entry in a match's event stream.
/// </summary>
public enum MultiplayerEventType : short
{
    MatchCreated = 0,
    MatchDisbanded = 1,
    PlayerJoined = 2,
    PlayerLeft = 3,
    PlayerKicked = 4,
    HostChanged = 5,
    GamePlayed = 6,
}