using BanchoNET.Core.Models.Mongo;

namespace BanchoNET.Core.Abstractions.Repositories.Histories;

/// <summary>
/// Read-only access to the legacy Mongo replay views history.
/// </summary>
public interface IReplaysRepository
{
    Task<List<ReplayViewsHistory>> GetReplaysHistories(byte mode);
}