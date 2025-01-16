using BanchoNET.Models.Mongo;

namespace BanchoNET.Abstractions.Repositories.Histories;

public interface IMultiplayerRepository
{
    Task InsertMatchHistory(MultiplayerMatch history);
    Task<int> GetMatchId();
    Task<MultiplayerMatch> GetMultiplayerMatch(int matchId);
    Task AddMatchAction(int matchId, ActionEntry action);
    Task AddMatchActions(int matchId, IEnumerable<ActionEntry> actions);
    Task MapStarted(int matchId, ScoresEntry entry);
    Task MapAborted(int matchId);
    Task MapCompleted(int matchId, List<ScoreEntry> scores);
}