using BanchoNET.Core.Models.Mongo;

namespace BanchoNET.Core.Abstractions.Repositories.Histories;

public interface IReplaysRepository
{
    Task InsertReplaysHistory(ReplayViewsHistory history);
    Task<List<int>> GetReplaysHistory(int playerId, byte mode);
    Task AddReplaysHistory(int playerId, byte mode, int entry);
}