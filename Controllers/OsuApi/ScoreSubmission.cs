using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;
using AkatsukiPp;
using BanchoNET.Objects;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Scores;
using BanchoNET.Packets;
using BanchoNET.Utils;
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
                throw new Exception("ovu! version mismatch");
			
            if (clientHash != player.ClientDetails.ClientHash)
                throw new Exception("Client hash mismatch");
			
            if (uniqueId1 != player.ClientDetails.UninstallMD5)
                throw new Exception($"UniqueId1 mismatch ({uniqueId1} != {player.ClientDetails.UninstallMD5})");

            if (uniqueId2 != player.ClientDetails.DiskSignatureMD5)
                throw new Exception($"UniqueId2 mismatch ({uniqueId2} != {player.ClientDetails.UninstallMD5})");

            var serverScoreChecksum = score.ComputeOnlineChecksum(osuVersion, clientHash, storyboardMD5);
            if (score.ClientChecksum != serverScoreChecksum)
                throw new Exception($"Online score checksum mismatch ({score.ClientChecksum} != {serverScoreChecksum})");

            if (beatmapMD5 != beatmapHash)
                throw new Exception($"Beatmap hash mismatch ({beatmapMD5} != {beatmapHash})");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ScoreSubmission] {e.Message}");

            await players.RestrictPlayer(player, "Mismatching hashes on score submission");
            return Ok("error: ban");
        }

        await players.UpdateLatestActivity(player);

        if (score.Mode != player.Status.Mode)
        {
            player.Status.Mode = score.Mode;
            player.Status.CurrentMods = score.Mods;
			
            if (!player.Restricted)
            {
                using var statsPacket = new ServerPackets();
                statsPacket.UserStats(player);
                _session.EnqueueToPlayers(statsPacket.GetContent());
            }
        }

        if (await scores.GetScore(score.ClientChecksum) != null)
        {
            Console.WriteLine($"[ScoreSubmission] {player.Username} tried to submit a duplicate score.");
            return Ok("error: no");
        }
		
        score.CalculateAccuracy();
        
        var bestWithMods = await scores.GetPlayerBestScoreWithModsOnMap(player, beatmapMD5, score.Mode, score.Mods);
        var prevBest = await scores.GetPlayerBestScoreOnMap(player, beatmapMD5, score.Mode);

        if (await beatmapHandler.EnsureLocalBeatmapFile(beatmap.MapId, beatmapMD5))
        {
            score.CalculatePerformance(beatmap.MapId);

            if (score.Passed)
            {
                score.ComputeSubmissionStatus(prevBest, bestWithMods);

                if (beatmap.Status != BeatmapStatus.LatestPending)
                    await scores.SetScoreLeaderboardPosition(beatmap, score, false);
            }
            else score.Status = SubmissionStatus.Failed;
        }
        else
        {
            score.PP = 0;
            score.Status = score.Passed ? SubmissionStatus.Submitted : SubmissionStatus.Failed;
        }

        score.TimeElapsed = score.Passed ? scoreTime : failTime;
		
        //TODO pp autoban(?)
        
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
				
                using var notification = new ServerPackets();
                notification.Notification(scoreNotification);
                player.Enqueue(notification.GetContent());

                await AnnounceNewFirstScore(score, player, beatmap);
            }
            
            await scores.SetScoresStatuses(prevBest, bestWithMods);
        }
        
        score.Player = player;
        player.RecentScore = await scores.InsertScore(score, beatmap.MD5, player);

        if (score.Passed)
        {
            if (replayFile.Length >= 24)
            {
                await using var fileStream = new FileStream(Storage.GetReplayPath(score.Id), FileMode.Create, FileAccess.ReadWrite);
                await replayFile.CopyToAsync(fileStream);
            }
            else
            {
                Console.WriteLine($"[ScoreSubmission] {player.Username} submitted a replay file with invalid length.");

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
        if (previousBest != null) await scores.SetScoreLeaderboardPosition(beatmap, previousBest, false);
        
        await RecalculatePlayerStats(beatmap, stats, player, score, previousBest, bestWithMods);
        await players.UpdatePlayerStats(player, score.Mode);

        if (!player.Restricted)
        {
            using var statsPacket = new ServerPackets();
            statsPacket.UserStats(player);
            _session.EnqueueToPlayers(statsPacket.GetContent());

            beatmap.Plays += 1;
            if (score.Passed)
                beatmap.Passes += 1;
			
            await beatmaps.UpdateBeatmapStats(beatmap);
        }
		
        string response;
        var achievements = "";
		
        if (!score.Passed || (int)score.Mode > 3 || (int)score.Mode < 0)
            response = "error: no";
        else
        {
            List<string> submissionCharts = [
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

        return Responses.BytesContentResult(response);
    }

    private async Task RecalculatePlayerStats(
        Beatmap beatmap,
        ModeStats stats,
        Player player,
        Score score,
        Score? previousBest,
        Score? bestWithMods)
    {
        if (score.Passed && beatmap.Status != BeatmapStatus.Qualified && beatmap.HasLeaderboard())
        {
            if (score.MaxCombo > stats.MaxCombo)
                stats.MaxCombo = score.MaxCombo;

            if (beatmap.AwardsPP() && score.Status == SubmissionStatus.Best)
            {
                var additionalRankedScore = score.TotalScore;

                if (previousBest != null)
                {
                    additionalRankedScore -= previousBest.TotalScore;

                    if (previousBest.Mods != bestWithMods?.Mods)
                    {
                        //if our previous best score does not have the same mods as our best score with mods,
                        //we always add 1 to our overall grade count because it's a new mod combo
                        if (score.Grade >= Grade.A)
                            stats.Grades[score.Grade] += 1;

                        //if our score has the same mods as our previous best score, we remove 1 from our
                        //overall grade count because it is not a new mod combo
                        if (score.Mods == previousBest.Mods)
                            if (previousBest.Grade >= Grade.A)
                                stats.Grades[previousBest.Grade] -= 1;
                    }
                    else
                    {
                        //if our previous best score has the same mods as our best score with mods, we only
                        //compare grades between those scores because previousBest and bestWithMods are the same
                        if (score.Grade != bestWithMods.Grade)
                        {
                            if (score.Grade >= Grade.A)
                                stats.Grades[score.Grade] += 1;

                            if (bestWithMods.Grade >= Grade.A)
                                stats.Grades[bestWithMods.Grade] -= 1;
                        }
                    }
                }
                else
                {
                    //no scores to compare with, just add 1
                    if (score.Grade >= Grade.A)
                        stats.Grades[score.Grade] += 1;
                }

                stats.RankedScore += additionalRankedScore;

                //TODO maybe recalculate top scores only when score is at least in top100?
                await players.RecalculatePlayerTopScores(player, score.Mode);
                await players.UpdatePlayerRank(player, score.Mode);
            }
            else if (beatmap.AwardsPP() && score.Status == SubmissionStatus.BestWithMods)
            {
                if (bestWithMods != null)
                {
                    //if our score is not best or submitted, we compare with our bestWithMods
                    if (score.Grade == bestWithMods.Grade) return;

                    if (score.Grade >= Grade.A)
                        stats.Grades[score.Grade] += 1;

                    if (bestWithMods.Grade >= Grade.A)
                        stats.Grades[bestWithMods.Grade] -= 1;
                }
                else
                {
                    //no previous best scores with mods, just add 1
                    if (score.Grade >= Grade.A)
                        stats.Grades[score.Grade] += 1;
                }
            }
        }
    }

    private async Task AnnounceNewFirstScore(Score score, Player player, Beatmap beatmap)
    {
        if (score.LeaderboardPosition == 1 && !player.Restricted)
        {
            var announceChannel = _session.GetChannel("#announce");
            var announcement = $@"\x01ACTION achieved #1 on {beatmap.Embed()} with {score.Acc:F2}% and {score.PP}pp.";
			
            var currentBest = await scores.GetBestBeatmapScore(beatmap, score.Mode);
					
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