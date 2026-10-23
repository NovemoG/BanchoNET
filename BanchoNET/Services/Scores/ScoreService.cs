using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Scores;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Scores;

public sealed partial class ScoreService(
    BanchoDbContext db,
    IScoresRepository scores,
    IPlayersRepository players,
    IBeatmapHandler beatmaps
) : IScoreService
{
    public Task<long> ReserveScoreId()
        => db.Database.SqlQueryRaw<long>(
               """SELECT nextval(pg_get_serial_sequence('"Scores"', 'Id')::regclass) AS "Value" """
           ).SingleAsync();

    public async Task<ScoreSubmissionResult> Submit(
        ScoreSubmission submission
    ) {
        var score = submission.Score;
        
        if (!Enum.IsDefined(score.Mode))
            throw new ArgumentException("A submission requires one of the four official rulesets.", nameof(submission));
        
        var legacy = score.LazerMods == null;
        if (legacy && string.IsNullOrWhiteSpace(score.OnlineChecksum))
            throw new ArgumentException("A stable submission requires its online checksum.", nameof(submission));
        
        if (!legacy && score.Id <= 0)
            throw new ArgumentException("A lazer submission requires its reserved score ID.", nameof(submission));

        var beatmap = await beatmaps.GetBeatmap(score.MapId)
            ?? throw new InvalidOperationException($"Beatmap {score.MapId} was not found.");

        ScoreSubmissionResult result;
        var recordedBeatmapPlay = false;
        
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            // Serialize retries before checking whether the play has already been saved.
            var identity = legacy ? $"stable:{score.PlayerId}:{score.OnlineChecksum}" : $"lazer:{score.Id}";
            
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({identity}, 0))"
            );
            
            await ScoreProcessing.LockPlayer(db, score.PlayerId, score.Mode);
            
            var mapKey = (long)score.PlayerId << 32 | (uint)score.MapId;
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({mapKey})");

            var existing = await db.Scores.FirstOrDefaultAsync(ScoreProcessing.MatchSubmission(score));
            if (existing != null && (existing.PlayerId != score.PlayerId
                                     || existing.MapId != score.MapId
                                     || existing.Mode != score.Mode
                                     || existing.LazerMods == null != legacy))
            {
                throw new InvalidOperationException("The submission identity belongs to a different score.");
            }

            var player = await db.Players.AsNoTracking().SingleAsync(p => p.Id == score.PlayerId);
            var stats = await db.Stats.SingleAsync(s => s.PlayerId == score.PlayerId && s.Mode == (byte)score.Mode);
           
            await db.Entry(stats).ReloadAsync();
            
            var previousStats = (StatsDto)db.Entry(stats).CurrentValues.ToObject();
            var previousRank = await players.GetPlayerGlobalRank(score.Mode, score.PlayerId);
            var (ppDisplay, sort) = await scores.GetLeaderboardPreferences(score.PlayerId);
            
            var previous = await scores.GetPlayerBestScoreOnMap(score.PlayerId, score.Mode, score.MapId, ppDisplay);
            var previousFirst = await scores.GetBestBeatmapScore(score.MapId, score.Mode);
            var isNew = existing == null;

            if (isNew)
            {
                if (!await beatmaps.EnsureLocalBeatmapFile(beatmap))
                    throw new InvalidOperationException($"Cannot process score: beatmap file {score.MapId} is unavailable.");

                score.Processed = true;
                score.PlayTimeRecorded = false;
                score.Acc = MathF.Round(score.Acc, 3, MidpointRounding.AwayFromZero);
                score.Ranked = score.Passed && (player.Privileges & 1) == 1 && beatmap.AwardsPp();
                score.ModKeys = string.Join(';', score.ModKeys.Split(';', StringSplitOptions.RemoveEmptyEntries).Order())
                                + (score.ModKeys.Length > 0 ? ";" : "");
                
                ScoreStandardisation.Populate(score);
                
                score.CalculatePerformance(new BeatmapDto
                {
                    Id = beatmap.Id, MD5 = beatmap.Checksum, Cs = beatmap.Cs, Ar = beatmap.Ar, Od = beatmap.Od
                });
                score.PP = MathF.Round(score.PP, 3, MidpointRounding.AwayFromZero);
                
                db.Scores.Add(score);
                await db.SaveChangesAsync();

                var candidates = await db.Scores
                    .Where(s => s.PlayerId == score.PlayerId
                                && s.MapId == score.MapId && s.Mode == score.Mode && s.BeatmapMD5 == score.BeatmapMD5
                                && (s.Status >= SubmissionStatus.BestWithMods
                                    || s.TotalScoreStatus >= SubmissionStatus.BestWithMods
                                    || s.Id == score.Id))
                    .ToListAsync();
                
                ScoreBestSelection.Assign(candidates);
                await db.SaveChangesAsync();
                
                ScoreProcessing.AddStatistics(stats, score, submission.PlayTimeSeconds);
                await ScoreStatistics.Recalculate(db, stats);

                if ((player.Privileges & 1) == 1)
                {
                    await RecordBeatmapPlay(score, beatmap, submission.MaximumStatistics);
                    recordedBeatmapPlay = true;
                }
                await players.UpdateLatestActivity(score.PlayerId, updateInactivity: true);
            }
            else
            {
                score = existing!;
            }

            var first = await scores.GetBestBeatmapScore(score.MapId, score.Mode);

            result = new ScoreSubmissionResult(score,
                stats,
                previousStats,
                previousRank,
                previous,
                await scores.GetLeaderboardPosition(score, ppDisplay, sort),
                previous == null ? 0 : await scores.GetLeaderboardPosition(previous, ppDisplay, sort),
                isNew,
                first?.Id == score.Id,
                previousFirst
            );
            await transaction.CommitAsync();
        }

        if (recordedBeatmapPlay)
        {
            beatmap.Plays++;
            beatmap.Passes += score.Passed ? 1 : 0;
            beatmap.Set.PlayCount++;
            
            if (score.Passed && submission.MaximumStatistics is { Count: > 0 } && !beatmap.MaxStatistics.Any(s => s.Value > 0))
                beatmap.MaxStatistics = submission.MaximumStatistics;
        }
        
        await RefreshRank(score.PlayerId, score.Mode);
        return result;
    }

    private async Task RefreshRank(
        int playerId,
        GameMode mode
    ) {
        // Read the latest totals under the same player lock before publishing them to Redis.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await ScoreProcessing.LockPlayer(db, playerId, mode);
        
        var stats = await db.Stats
            .Include(s => s.Player)
            .SingleAsync(s => s.PlayerId == playerId && s.Mode == (byte)mode);
        
        await db.Entry(stats).ReloadAsync();
        
        await players.UpdatePlayerRank(playerId, (stats.Player.Privileges & 1) == 0,
            stats.Player.Country, stats, mode);
        await transaction.CommitAsync();
    }

    private async Task RecordBeatmapPlay(
        ScoreDto score,
        Beatmap beatmap,
        Dictionary<HitResult, int>? maximumStatistics
    ) {
        await db.Beatmaps
            .Where(b => b.Id == score.MapId)
            .ExecuteUpdateAsync(p => p
                .SetProperty(b => b.Plays, b => b.Plays + 1)
                .SetProperty(b => b.Passes, b => b.Passes + (score.Passed ? 1 : 0)));

        await db.Beatmapsets
            .Where(b => b.Id == beatmap.BeatmapsetId)
            .ExecuteUpdateAsync(p => p
                .SetProperty(b => b.PlayCount, b => b.PlayCount + 1));
        
        var play = await db.BeatmapPlays.SingleOrDefaultAsync(p => p.PlayerId == score.PlayerId && p.BeatmapId == score.MapId);
        if (play == null)
            db.BeatmapPlays.Add(new BeatmapPlays { PlayerId = score.PlayerId, BeatmapId = score.MapId, Plays = 1 });
        else
            play.Plays++;
        
        if (score.Passed && maximumStatistics is { Count: > 0 })
        {
            var map = await db.Beatmaps.SingleAsync(b => b.Id == score.MapId);
            if (!map.MaximumStatistics.Any(s => s.Value > 0))
                map.MaximumStatistics = maximumStatistics;
        }
        await db.SaveChangesAsync();
    }
}