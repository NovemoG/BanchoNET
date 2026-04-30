namespace BanchoNET.Core.Models.Lazer.Multiplayer.Matchmaking;

public enum MatchmakingStage
{
    WaitingForClientsJoin,
    RoundWarmupTime,
    UserBeatmapSelect,
    ServerBeatmapFinalised,
    WaitingForClientsBeatmapDownload,
    GameplayWarmupTime,
    Gameplay,
    ResultsDisplaying,
    Ended,
}