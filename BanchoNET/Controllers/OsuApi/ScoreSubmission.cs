using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;
using AkatsukiPp;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
    [HttpPost("osu-submit-modular-selector.php")]
    public async Task<IActionResult> SubmitScore(
        [FromForm(Name = "ft")] int failTime,
        [FromForm(Name = "bmk")] string beatmapHash,
        [FromForm(Name = "iv")] string ivB64,
        [FromForm(Name = "c1")] string uniqueIds,
        [FromForm(Name = "st")] int scoreTime,
        [FromForm(Name = "pass")] string passwordMD5,
        [FromForm(Name = "osuver")] string osuVersion,
        [FromForm(Name = "s")] byte[] clientHashB64,
        [FromForm(Name = "score")] string scoreDataB64,
        [FromForm(Name = "score")] IFormFile? replayFile = null,
        [FromForm(Name = "sbk")] string? storyboardMD5 = null)
    {
        if (string.IsNullOrEmpty(scoreDataB64) || replayFile == null)
            return Ok();
		
        var decryptedData = DecryptScoreData(osuVersion, ivB64, scoreDataB64, clientHashB64);
        if (decryptedData == null)
            return Ok();

        var scoreData = decryptedData.Value.scoreData;
        var clientHash = decryptedData.Value.clientHash;

        var beatmapMD5 = scoreData[0];
        var beatmap = await beatmaps.GetBeatmap(beatmapMD5: beatmapMD5);
        if (beatmap == null)
            return Ok("error: beatmap");

        var username = scoreData[1];
        if (username[^1] == ' ')
            username = username[..^1];

        var player = await players.GetPlayerFromLogin(username, passwordMD5);
        if (player == null)
            return Ok();

        var score = new Score(scoreData[2..], beatmap, player);
		
        var uniqueIdMD5s = uniqueIds.Split('|', 2);
        var uniqueId1 = uniqueIdMD5s[0].CreateMD5();
        var uniqueId2 = uniqueIdMD5s[1].CreateMD5();

        try
        {
            var versionDate = DateTime.ParseExact(osuVersion[..8], "yyyyMMdd", null);
            
            if (versionDate != player.ClientDetails.OsuVersion.Date)
                throw new Exception("osu! version mismatch");
			
            if (clientHash != player.ClientDetails.ClientHash)
                throw new Exception("Client hash mismatch");
			
            if (uniqueId1 != player.ClientDetails.UninstallMD5)
                throw new Exception($"UniqueId1 mismatch ({uniqueId1} != {player.ClientDetails.UninstallMD5})");

            if (uniqueId2 != player.ClientDetails.DiskSignatureMD5)
                throw new Exception($"UniqueId2 mismatch ({uniqueId2} != {player.ClientDetails.UninstallMD5})");

            var serverScoreChecksum = score.ComputeOnlineChecksum(osuVersion, clientHash, storyboardMD5 ?? "");
            if (score.ClientChecksum != serverScoreChecksum)
                throw new Exception($"Online score checksum mismatch ({score.ClientChecksum} != {serverScoreChecksum})");

            if (beatmapMD5 != beatmapHash)
                throw new Exception($"Beatmap hash mismatch ({beatmapMD5} != {beatmapHash})");
        }
        catch (Exception e)
        {
            logger.LogError("Mismatching hashes on score submission", e);

            await players.RestrictPlayer(player, "Mismatching hashes on score submission");
            return Ok("error: ban");
        }

        if (await scores.ScoreExists(score.ClientChecksum))
        {
            logger.LogWarning($"{player.Username} tried to submit a duplicate score.");
            return Ok("error: no");
        }
        
        await players.UpdateLatestActivity(player);

        if (score.Mode != player.Status.Mode)
        {
            player.Status.Mode = score.Mode;
            player.Status.CurrentMods = score.Mods;
			
            if (!player.Restricted)
            {
                session.EnqueueToPlayers(new ServerPackets()
                    .UserStats(player)
                    .FinalizeAndGetContent());
            }
        }
        score.CalculateAccuracy();
        
        var prevBest = await scores.GetPlayerBestScoreOnMap(player.Id, beatmapMD5, score.Mode);
        var bestWithMods = prevBest != null && score.Mods != prevBest.Mods
            ? await scores.GetPlayerBestScoreWithModsOnMap(player.Id, beatmapMD5, score.Mode, score.Mods)
            : null;
        
        if (await beatmapHandler.EnsureLocalBeatmapFile(beatmap.MapId, beatmapMD5))
        {
            score.CalculatePerformance(beatmap.MapId);

            if (score.Passed)
            {
                ComputeSubmissionStatus(score, prevBest, bestWithMods);
                
                if (beatmap.Status != BeatmapStatus.LatestPending)
                    await scores.SetScoreLeaderboardPosition(beatmap.MD5, score, false);
            }
            else score.Status = SubmissionStatus.Failed;
            
            await scores.UpdateScoreStatus(prevBest);
            await scores.UpdateScoreStatus(bestWithMods);
        }
        else
        {
            score.PP = 0;
            score.Status = score.Passed ? SubmissionStatus.Submitted : SubmissionStatus.Failed;
        }

        score.TimeElapsed = score.Passed ? scoreTime : failTime;
        
        if (score.Status == SubmissionStatus.Best)
        {
            if (beatmap.HasLeaderboard())
            {
                var scoreNotification = $"You achieved #{score.LeaderboardPosition} on a leaderboard!\n";
				
                if (AppSettings.DisplayPPInNotification)
                    scoreNotification += $"({score.PP:F2}pp)";

                if (AppSettings.DisplayScoreInNotification)
                    scoreNotification += $" ({score.TotalScore.SplitNumber()} score)";

                //Subject to change
                if (AppSettings.DisplayPPInNotification
                    && (score.Misses > 0 || beatmap.MaxCombo - score.MaxCombo > 15))
                {
                    var fcPP = AkatsukiPpMethods.ComputeNoMissesScorePp(
                        beatmap.MapId,
                        score,
                        beatmap.MaxCombo);
					
                    scoreNotification += $"\n[{fcPP:F2}pp if FC]";
                }
                
                player.Enqueue(new ServerPackets()
                    .Notification(scoreNotification)
                    .FinalizeAndGetContent());

                await AnnounceNewFirstScore(score, player, beatmap);
            }
        }
        
        score.Player = player;
        player.RecentScore = await scores.InsertScore(score, beatmap.MD5, player.Restricted);

        if (score.Passed)
        {
            if (replayFile.Length >= 24)
            {
                await using var fileStream = new FileStream(Storage.GetReplayPath(score.Id), FileMode.Create, FileAccess.ReadWrite);
                await replayFile.CopyToAsync(fileStream);
            }
            else
            {
                logger.LogWarning($"{player.Username} submitted a replay file with invalid length.");

                if (!player.Restricted)
                    await players.RestrictPlayer(player, "Submitted score with invalid replay length");
            }
        }

        var stats = player.Stats[score.Mode];
        var prevStats = stats.Copy();

        stats.PlayTime += (int)MathF.Floor(score.TimeElapsed / 1000f);
        stats.PlayCount += 1; 
        stats.TotalScore += score.TotalScore;
        stats.UpdateHits(score);
        
        var previousBest = score.PreviousBest;
        if (previousBest != null) await scores.SetScoreLeaderboardPosition(beatmap.MD5, previousBest, false);
        
        await RecalculatePlayerStats(beatmap, stats, player, score, previousBest, bestWithMods);
        await players.UpdatePlayerStats(player, score.Mode);

        if (!player.Restricted)
        {
            session.EnqueueToPlayers(new ServerPackets()
                .UserStats(player)
                .FinalizeAndGetContent());

            beatmap.Plays += 1;
            if (score.Passed)
                beatmap.Passes += 1;
			
            await beatmaps.UpdateBeatmapPlayCount(beatmap);
        }
		
        string response;
        var achievements = "";
        
        if (score.Passed && (int)score.Mode <= 3 && (int)score.Mode >= 0)
        {
            List<string> submissionCharts =
            [
                $"beatmapId:{beatmap.MapId}",
                $"beatmapSetId:{beatmap.SetId}",
                $"beatmapPlaycount:{(int)beatmap.Plays}",
                $"beatmapPasscount:{(int)beatmap.Passes}",
                $"approvedDate:{beatmap.LastUpdate:yyyy-MM-dd HH:mm:ss}",
                "\n",
                "chartId:beatmap",
                $"chartUrl:{beatmap.Set?.Url()}",
                "chartName:Beatmap Ranking",
                ChartEntry("rank", previousBest?.LeaderboardPosition, score.LeaderboardPosition),
                ChartEntry("rankedScore", previousBest?.TotalScore, score.TotalScore),
                ChartEntry("totalScore", previousBest?.TotalScore, score.TotalScore),
                ChartEntry("maxCombo", previousBest?.MaxCombo, score.MaxCombo),
                ChartEntry("accuracy",
                    previousBest == null ? null : MathF.Round(previousBest.Acc, 2),
                    MathF.Round(score.Acc, 2)),
                ChartEntry("pp", previousBest?.PP, score.PP),
                $"onlineScoreId:{score.Id}",
                "\n",
                "chartId:overall",
                $"chartUrl:https://{AppSettings.Domain}/u/{player.Id}",
                "chartName:Overall Ranking",
                ChartEntry("rank", prevStats.Rank, stats.Rank),
                ChartEntry("rankedScore", prevStats.RankedScore, stats.RankedScore),
                ChartEntry("totalScore", prevStats.TotalScore, stats.TotalScore),
                ChartEntry("maxCombo", prevStats.MaxCombo, stats.MaxCombo),
                ChartEntry("accuracy",
                    MathF.Round(prevStats.Accuracy, 2),
                    MathF.Round(stats.Accuracy, 2)),
                ChartEntry("pp", prevStats.PP, stats.PP),
                $"achievements-new:{achievements}",
            ];

            if (beatmap.AwardsPP() && !player.Restricted)
            {
                //var unlockedAchievements = new List<Achievement>();
                //TODO server achievements
                //TODO fetch player achievements
                //TODO achievements string
            }

            response = string.Join("|", submissionCharts);
        }
        else response = "error: no";

        return Responses.BytesContentResult(response);
    }
    
    private static void ComputeSubmissionStatus(
        Score newScore,
        Score? prevBest,
        Score? bestWithMods)
    {
        newScore.Status = SubmissionStatus.Submitted;
        
        if (newScore.IsBetterThan(prevBest))
        {
            if (prevBest != null)
            {
                // if new score beats prevBest and has different mods,
                // prevBest becomes bestWithMods
                prevBest.Status = newScore.Mods != prevBest.Mods
                    ? SubmissionStatus.BestWithMods
                    : SubmissionStatus.Submitted;
                
                newScore.PreviousBest = prevBest;
            }
            
            newScore.Status = SubmissionStatus.Best;
        }
        else
        {
            // if it didn't beat prevBest, check if it bestWithMods exists and set
            // status accordingly (it is going to be properly checked later)
            if (bestWithMods == null)
                newScore.Status = SubmissionStatus.BestWithMods;
            
            newScore.PreviousBest = prevBest;
        }

        if (bestWithMods == null || !newScore.IsBetterThan(bestWithMods)) return;
        
        // if it exists and new score is better set bestWithMods to Submitted
        bestWithMods.Status = SubmissionStatus.Submitted;
        
        // this check is for if new score is not better than prevBest but is
        // better than bestWithMods
        if (newScore.Status != SubmissionStatus.Best)
            newScore.Status = SubmissionStatus.BestWithMods;
    }

    private async Task RecalculatePlayerStats(
        Beatmap beatmap,
        ModeStats stats,
        Player player,
        Score score,
        Score? prevBest,
        Score? bestWithMods)
    {
        if (!score.Passed || !beatmap.AwardsPP())
            return;
        
        if (score.MaxCombo > stats.MaxCombo)
            stats.MaxCombo = score.MaxCombo;
        
        if (score.Status == SubmissionStatus.Best)
        {
            var oldBestScore = 0;
            
            if (prevBest != null)
            {
                // our current score is best, so if prevbest is submitted subtract,
                // otherwise our score should still count because it is in the modded
                // leaderboard; then if current score beat both prevBest and bestWithMods
                // but prevBest is BestWithMods we subtract bestWithMods
                if (prevBest is { Status: SubmissionStatus.Submitted, Grade: >= Grade.A })
                    stats.Grades[prevBest.Grade] -= 1;
                else if (bestWithMods != null)
                    stats.Grades[bestWithMods.Grade] -= 1;
                
                oldBestScore = prevBest.TotalScore;
            }
            
            stats.RankedScore += score.TotalScore - oldBestScore;
            
            if (score.Grade >= Grade.A)
                stats.Grades[score.Grade] += 1;
            
            //TODO maybe recalculate top scores only when score is at least in top100?
            await players.RecalculatePlayerTopScores(player, score.Mode);
            await players.UpdatePlayerRank(player, score.Mode);
        }
        else if (score.Status == SubmissionStatus.BestWithMods)
        {
            // if our score didnt beat prevBest but beat bestWithMods subtract
            if (bestWithMods is { Grade: >= Grade.A })
                stats.Grades[bestWithMods.Grade] -= 1;
            
            if (score.Grade >= Grade.A)
                stats.Grades[score.Grade] += 1;
        }
    }

    private async Task AnnounceNewFirstScore(Score score, Player player, Beatmap beatmap)
    {
        if (score.LeaderboardPosition == 1 && !player.Restricted)
        {
            var announceChannel = session.GetChannel("#announce");
            var announcement = $@"\x01ACTION achieved #1 on {beatmap.Embed()} with {score.Acc:F2}% and {score.PP}pp.";
			
            var currentBest = await scores.GetBestBeatmapScore(beatmap.MD5, score.Mode);
					
            if (score.Mods > 0)
                announcement = announcement.Insert(0, $"+{score.Mods}");

            if (currentBest != null)
                if (currentBest.PlayerId != score.PlayerId)
                    announcement += $" (Previous #1: [https://{AppSettings.Domain}/u/{currentBest.PlayerId} {currentBest.Player.Username}])";
					
            announceChannel?.SendMessage(new Message
            {
                Sender = player.Username,
                Content = announcement,
                Destination = "",
                SenderId = player.Id
            });
        }
    }
	
    private static (string[] scoreData, string clientHash)? DecryptScoreData(
        string osuVersion,
        string ivB64,
        string scoreDataB64,
        byte[] clientHashB64)
    {
        if (string.IsNullOrEmpty(osuVersion) || ivB64.Length == 0) return null;
		
        var scoreB64 = Convert.FromBase64String(scoreDataB64);
        var key = Encoding.UTF8.GetBytes($"osu!-scoreburgr---------{osuVersion}");
        var iv = Convert.FromBase64String(ivB64);
		
        var engine = new RijndaelEngine(256);
        var blockCipher = new CbcBlockCipher(engine);
        var cipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());
        var keyParam = new KeyParameter(key);
        var keyParamWithIv = new ParametersWithIV(keyParam, iv, 0, 32);
		
        return (
            DecipherBytes(cipher, keyParamWithIv, scoreB64).Split(':'),
            DecipherBytes(cipher, keyParamWithIv, clientHashB64));
    }

    private static string DecipherBytes(
        PaddedBufferedBlockCipher cipher,
        ParametersWithIV keyWithIv,
        byte[] bytesToDecipher)
    {
        cipher.Init(false, keyWithIv);
        var comparisonBytes = new byte[cipher.GetOutputSize(bytesToDecipher.Length)];
        var length = cipher.ProcessBytes(bytesToDecipher, comparisonBytes, 0);
        cipher.DoFinal(comparisonBytes, length);

        return Encoding.UTF8.GetString(comparisonBytes).TrimEnd('\0');
    }

    private static string ChartEntry(string name, float? before, float? after)
    {
        return $"{name}Before:{before.ToString() ?? ""}|{name}After:{after.ToString() ?? ""}";
    }
}