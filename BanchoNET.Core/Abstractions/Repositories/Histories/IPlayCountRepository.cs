using BanchoNET.Core.Models.Mongo;

namespace BanchoNET.Core.Abstractions.Repositories.Histories;

public interface IPlayCountRepository
{
    Task InsertPlayCountHistory(PlayCountHistory history);
    Task<List<int>> GetPlayCountHistory(int playerId, byte mode);
    Task AddPlayCountHistory(int playerId, byte mode, int entry);
}