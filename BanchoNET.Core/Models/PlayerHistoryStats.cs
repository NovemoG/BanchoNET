namespace BanchoNET.Core.Models;

public readonly struct PlayerHistoryStats(int playerId, int playCount, int replayViews)
{
    public readonly int PlayerId = playerId;
    public readonly int PlayCount = playCount;
    public readonly int ReplayViews = replayViews;

    public void Deconstruct(out int playerId, out int playCount, out int replayViews)
    {
        playerId = PlayerId;
        playCount = PlayCount;
        replayViews = ReplayViews;
    }
}