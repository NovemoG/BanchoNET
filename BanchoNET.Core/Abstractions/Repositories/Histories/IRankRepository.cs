using BanchoNET.Core.Models.Mongo;

namespace BanchoNET.Core.Abstractions.Repositories.Histories;

public interface IRankRepository
{
    Task InsertRankHistory(RankHistory history);
    Task<PeakRank> GetPeakRank(int playerId, byte mode);
    Task<List<int>> GetRankHistory(int playerId, byte mode);
    Task AddRankHistory(int playerId, byte mode, int entry);
    Task UpdatePeakRank(int playerId, byte mode, PeakRank peakRank);
}