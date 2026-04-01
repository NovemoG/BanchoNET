namespace BanchoNET.Core.Abstractions.Services;

public interface IBackgroundTasks
{
    Task UpdateLazer(CancellationToken ct);
    Task AppendPlayerRankHistory(CancellationToken ct);
    Task AppendPlayerMonthlyHistory(CancellationToken ct);
    Task DeleteUnnecessaryScores(CancellationToken ct);
    Task CheckExpiringSupporters(CancellationToken ct);
    Task MarkInactivePlayers(CancellationToken ct);
    void UpdateBotStatus();
}