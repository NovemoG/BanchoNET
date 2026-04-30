namespace BanchoNET.Core.Models.Lazer.Multiplayer;

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