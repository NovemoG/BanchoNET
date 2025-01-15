namespace BanchoNET.Abstractions.Services;

public interface IBackgroundTasks : IInitiable
{
    void ClearPasswordsCache();
    Task AppendPlayerRankHistory();
    Task AppendPlayerMonthlyHistory();
    Task DeleteUnnecessaryScores();
    Task CheckExpiringSupporters();
    void UpdateBotStatus();
}