namespace BanchoNET.Core.Abstractions.Services;

public readonly record struct OnlineSample(
    DateTime At,
    int Count
);

public interface IServerStatsService
{
    Task<int> GetOnlineCount();
    
    int GetActiveGameCount();
    
    Task SampleOnlineCount();
    
    Task<List<OnlineSample>> GetOnlineHistory();
}