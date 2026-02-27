namespace BanchoNET.Core.Abstractions.Services;

public interface IBackgroundTasks : IInitiable
{
    Task AppendPlayerRankHistory();
    Task AppendPlayerMonthlyHistory();
    Task DeleteUnnecessaryScores();
    Task CheckExpiringSupporters();
    Task MarkInactivePlayers();
    void UpdateBotStatus();
}