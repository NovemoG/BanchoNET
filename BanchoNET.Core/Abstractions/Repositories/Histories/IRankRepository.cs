using BanchoNET.Core.Models.Mongo;

namespace BanchoNET.Core.Abstractions.Repositories.Histories;

/// <summary>
/// Read-only access to the legacy Mongo rank history.
/// </summary>
public interface IRankRepository
{
    Task<List<RankHistoryEntry>> GetRankHistories(byte mode);
}