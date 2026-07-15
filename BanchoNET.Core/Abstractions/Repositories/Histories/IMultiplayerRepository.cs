using BanchoNET.Core.Models.Mongo;

namespace BanchoNET.Core.Abstractions.Repositories.Histories;

public interface IMultiplayerRepository
{
    Task<long> InsertMatchHistory(MultiplayerMatch history);
    Task<MultiplayerMatch> GetMultiplayerMatch(long matchId);
    Task AddMatchAction(long matchId, ActionEntry action);
    Task AddMatchActions(long matchId, IEnumerable<ActionEntry> actions);
    Task MapStarted(long matchId, ScoresEntry entry);
    Task MapAborted(long matchId);
    Task MapCompleted(long matchId, List<ScoreEntry> scores);
}