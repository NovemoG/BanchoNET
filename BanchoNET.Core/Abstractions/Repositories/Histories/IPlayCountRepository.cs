using BanchoNET.Core.Models.Mongo;

namespace BanchoNET.Core.Abstractions.Repositories.Histories;

/// <summary>
/// Read-only access to the legacy Mongo play count history.
/// </summary>
public interface IPlayCountRepository
{
    Task<List<PlayCountHistory>> GetPlayCountHistories(byte mode);
}