using BanchoNET.Models.Mongo;

namespace BanchoNET.Abstractions.Repositories.Histories;

public interface IReplaysRepository
{
    Task InsertReplaysHistory(ReplayViewsHistory history);
    Task<List<int>> GetReplaysHistory(int playerId, byte mode);
    Task AddReplaysHistory(int playerId, byte mode, int entry);
}