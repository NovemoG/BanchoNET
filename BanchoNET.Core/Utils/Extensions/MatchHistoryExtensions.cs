using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Multiplayer;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Multiplayer;

namespace BanchoNET.Core.Utils.Extensions;

public static class MatchHistoryExtensions
{
    public static MatchSummaryResponse ToSummary(
        this MultiplayerMatchDto match
    ) {
        return new MatchSummaryResponse
        {
            Id = match.Id,
            Name = match.Name,
            HostId = match.HostId,
            Mode = ModeName(match.Mode),
            StartTime = match.StartTime,
            EndTime = match.EndTime,
            ParticipantCount = match.Participants.Count
        };
    }
    
    public static MatchResponse ToResponse(
        this MultiplayerMatchDto match
    ) {
        return new MatchResponse
        {
            Match = match.ToSummary(),
            Users = match.Participants
                .Select(p => new BasicApiPlayer(p.Player))
                .ToList(),
            Events = match.Events.Select(e => new MatchEventResponse
            {
                Id = e.Id,
                Type = e.Type,
                UserId = e.UserId,
                CreatedAt = e.CreatedAt,
                Game = e.Type == MultiplayerEventType.GamePlayed && e.Game != null
                    ? e.Game.ToResponse()
                    : null
            }).ToList()
        };
    }

    private static MatchGameResponse ToResponse(
        this MultiplayerGameDto game
    ) {
        return new MatchGameResponse
        {
            Id = game.Id,
            BeatmapId = game.BeatmapId,
            BeatmapMD5 = game.BeatmapMD5,
            BeatmapName = game.BeatmapName,
            Mode = ModeName(game.Mode),
            WinCondition = game.WinCondition,
            LobbyType = game.LobbyType,
            Mods = game.Mods,
            StartTime = game.StartTime,
            EndTime = game.EndTime,
            Aborted = game.Aborted,
            ForceCompleted = game.ForceCompleted,
            Scores = game.Scores
                .OrderByDescending(s => s.TotalScore)
                .Select(s => new MatchScoreResponse
                {
                    UserId = s.PlayerId,
                    ScoreId = s.ScoreId,
                    Team = s.Team,
                    TotalScore = s.TotalScore,
                    MaxCombo = s.MaxCombo,
                    Accuracy = s.Accuracy,
                    Grade = s.Grade,
                    Mods = (int)s.Mods,
                    Count300 = s.Count300,
                    Count100 = s.Count100,
                    Count50 = s.Count50,
                    CountGeki = s.Gekis,
                    CountKatu = s.Katus,
                    CountMiss = s.Misses,
                    Failed = s.Failed
                })
                .ToList()
        };
    }

    private static string ModeName(byte mode) =>
        EnumExtensions.FromModeMap.GetValueOrDefault(((GameMode)mode).AsVanilla(), "osu");
}