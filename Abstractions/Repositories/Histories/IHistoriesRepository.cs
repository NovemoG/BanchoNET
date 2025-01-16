namespace BanchoNET.Abstractions.Repositories.Histories;

public interface IHistoriesRepository : 
    IMultiplayerRepository,
    IRankRepository,
    IReplaysRepository,
    IPlayCountRepository
{
    Task DeletePlayerData(int playerId);
}