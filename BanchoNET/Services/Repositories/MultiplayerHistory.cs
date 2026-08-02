using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Multiplayer;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class MultiplayerHistoryRepository(BanchoDbContext dbContext) : IMultiplayerHistoryRepository
{
    public async Task<long> CreateMatch(
        string name,
        int? hostId,
        byte mode,
        DateTimeOffset startTime,
        CancellationToken ct = default
    ) {
        var match = new MultiplayerMatchDto
        {
            Name = name.Length > 128 ? name[..128] : name,
            HostId = hostId,
            Mode = mode,
            StartTime = startTime
        };

        dbContext.MultiplayerMatches.Add(match);
        await dbContext.SaveChangesAsync(ct);

        await AppendEvent(match.Id, MultiplayerEventType.MatchCreated, hostId, ct: ct);

        return match.Id;
    }

    public async Task CloseMatch(
        long matchId,
        DateTimeOffset endTime,
        CancellationToken ct = default
    ) {
        var closed = await dbContext.MultiplayerMatches
            .Where(m => m.Id == matchId && m.EndTime == null)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.EndTime, endTime), ct);

        // A match that was already closed does not get a second disband event
        if (closed == 0) return;

        await AppendEvent(matchId, MultiplayerEventType.MatchDisbanded, ct: ct);
    }

    public async Task SetHost(
        long matchId,
        int? hostId,
        CancellationToken ct = default
    ) {
        await dbContext.MultiplayerMatches
            .Where(m => m.Id == matchId)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.HostId, hostId), ct);

        await AppendEvent(matchId, MultiplayerEventType.HostChanged, hostId, ct: ct);
    }

    public async Task<long> StartGame(
        MultiplayerGameDto game,
        CancellationToken ct = default
    ) {
        if (game.BeatmapName.Length > 512)
            game.BeatmapName = game.BeatmapName[..512];

        dbContext.MultiplayerGames.Add(game);
        await dbContext.SaveChangesAsync(ct);

        await AppendEvent(game.MatchId, MultiplayerEventType.GamePlayed, gameId: game.Id, ct: ct);

        return game.Id;
    }

    public async Task CloseGame(
        long gameId,
        DateTimeOffset endTime,
        bool aborted = false,
        bool forceCompleted = false,
        CancellationToken ct = default
    ) {
        await dbContext.MultiplayerGames
            .Where(g => g.Id == gameId && g.EndTime == null)
            .ExecuteUpdateAsync(g => g
                .SetProperty(x => x.EndTime, endTime)
                .SetProperty(x => x.Aborted, aborted)
                .SetProperty(x => x.ForceCompleted, forceCompleted), ct);
    }

    public async Task AppendScore(
        MultiplayerScoreDto score,
        CancellationToken ct = default
    ) {
        const string sql =
            """
            INSERT INTO "MultiplayerScores" (
                "GameId", "PlayerId", "ScoreId", "Team", "TotalScore", "MaxCombo", "Accuracy",
                "Grade", "Mods", "Count300", "Count100", "Count50", "Gekis", "Katus", "Misses", "Failed"
            )
            VALUES (
                {0}, {1}, {2}, {3}, {4}, {5}, {6},
                {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}
            )
            ON CONFLICT DO NOTHING
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, [
            score.GameId,
            score.PlayerId,
            score.ScoreId as object ?? DBNull.Value,
            (short)score.Team,
            score.TotalScore,
            score.MaxCombo,
            (decimal)score.Accuracy,
            (short)score.Grade,
            (int)score.Mods,
            score.Count300,
            score.Count100,
            score.Count50,
            score.Gekis,
            score.Katus,
            score.Misses,
            score.Failed
        ], ct);
    }

    public async Task RecordJoin(
        long matchId,
        int playerId,
        CancellationToken ct = default
    ) {
        const string sql =
            """
            INSERT INTO "MultiplayerParticipants" ("MatchId", "PlayerId")
            VALUES ({0}, {1})
            ON CONFLICT DO NOTHING
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, [matchId, playerId], ct);

        await AppendEvent(matchId, MultiplayerEventType.PlayerJoined, playerId, ct: ct);
    }

    public Task RecordLeave(
        long matchId,
        int playerId,
        bool kicked = false,
        CancellationToken ct = default
    ) {
        return AppendEvent(
            matchId,
            kicked ? MultiplayerEventType.PlayerKicked : MultiplayerEventType.PlayerLeft,
            playerId,
            ct: ct
        );
    }

    public async Task<List<MultiplayerMatchDto>> GetMatches(
        long? beforeId,
        int limit,
        CancellationToken ct = default
    ) {
        var query = dbContext.MultiplayerMatches.AsNoTracking();

        if (beforeId.HasValue)
            query = query.Where(m => m.Id < beforeId.Value);

        return await query
            .Include(m => m.Participants)
            .OrderByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<MultiplayerMatchDto?> GetMatch(
        long matchId,
        CancellationToken ct = default
    ) {
        var match = await dbContext.MultiplayerMatches
            .AsNoTracking()
            .Include(m => m.Participants)
                .ThenInclude(p => p.Player)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match == null) return null;
        
        var games = await dbContext.MultiplayerGames
            .AsNoTracking()
            .Where(g => g.MatchId == matchId)
            .Include(g => g.Scores)
            .ToDictionaryAsync(g => g.Id, ct);

        var events = await dbContext.MultiplayerEvents
            .AsNoTracking()
            .Where(e => e.MatchId == matchId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

        foreach (var matchEvent in events)
        {
            if (matchEvent.GameId is { } gameId && games.TryGetValue(gameId, out var game))
                matchEvent.Game = game;
        }

        match.Events = events;
        match.Games = games.Values;

        return match;
    }

    public async Task<List<MultiplayerMatchDto>> GetPlayerMatches(
        int playerId,
        int offset,
        int limit,
        CancellationToken ct = default
    ) {
        var matchIds = await dbContext.MultiplayerParticipants
            .AsNoTracking()
            .Where(p => p.PlayerId == playerId)
            .OrderByDescending(p => p.MatchId)
            .Skip(offset)
            .Take(limit)
            .Select(p => p.MatchId)
            .ToListAsync(ct);

        if (matchIds.Count == 0) return [];

        return await dbContext.MultiplayerMatches
            .AsNoTracking()
            .Where(m => matchIds.Contains(m.Id))
            .Include(m => m.Participants)
            .OrderByDescending(m => m.Id)
            .ToListAsync(ct);
    }

    public async Task<int> GetPlayerMatchesCount(
        int playerId,
        CancellationToken ct = default
    ) {
        return await dbContext.MultiplayerParticipants
            .AsNoTracking()
            .CountAsync(p => p.PlayerId == playerId, ct);
    }

    private async Task AppendEvent(
        long matchId,
        MultiplayerEventType type,
        int? userId = null,
        long? gameId = null,
        CancellationToken ct = default
    ) {
        dbContext.MultiplayerEvents.Add(new MultiplayerEventDto
        {
            MatchId = matchId,
            Type = type,
            UserId = userId,
            GameId = gameId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(ct);
    }
}