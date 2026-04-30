namespace BanchoNET.Core.Models.Lazer.Multiplayer.RankedPlay;

public enum RankedPlayStage
{
    WaitForJoin,
    RoundWarmup,
    CardDiscard,
    FinishCardDiscard,
    CardPlay,
    FinishCardPlay,
    GameplayWarmup,
    Gameplay,
    Results,
    Ended
}