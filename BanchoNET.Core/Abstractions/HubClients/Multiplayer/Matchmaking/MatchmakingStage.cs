namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

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