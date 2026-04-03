namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer;

public enum MultiplayerUserState
{
    Idle,
    Ready,
    WaitingForLoad,
    Loaded,
    ReadyForGameplay,
    Playing,
    FinishedPlay,
    Results,
    Spectating
}