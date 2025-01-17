using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Scores;

namespace BanchoNET.Abstractions.Repositories;

public interface IScoresRepository
{
    Task<ScoreDto?> GetScore(long id);
    Task<bool> ScoreExists(string checksum);
    Task<Score> InsertScore(Score score, string beatmapMD5, bool isPlayerRestricted);
    Task<Score?> GetPlayerRecentScore(int playerId);
    Task<List<ScoreDto>> GetPlayerRecentScores(int playerId, int start, int count = 10);
    Task<List<ScoreDto>> GetMultiplayerScores(List<int> playerId, DateTime finishDate);
    Task UpdateScoreStatus(Score? score);
    Task UpdateScoreStatus(long id, SubmissionStatus newStatus);
    Task<Score?> GetPlayerBestScoreOnMap(int playerId, string beatmapMD5, GameMode mode);
    Task<Score?> GetPlayerBestScoreWithModsOnMap(int playerId, string beatmapMD5, GameMode mode, Mods mods);
    Task<ScoreDto?> GetBestBeatmapScore(string beatmapMD5, GameMode mode);
    Task SetScoreLeaderboardPosition(string beatmapMD5, Score score, bool withMods, Mods mods = Mods.None);
    Task<List<ScoreDto>> GetBeatmapLeaderboard(string beatmapMD5, GameMode mode, LeaderboardType type, Mods mods, Player player);
    Task ToggleBeatmapScoresVisibility(string beatmapMD5, bool visible);
    Task<List<long>> DeleteOldScores(short differenceInHours = 48);
}