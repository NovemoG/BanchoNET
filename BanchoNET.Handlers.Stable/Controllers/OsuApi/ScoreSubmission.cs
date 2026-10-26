using System.Text;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Models.Stable.Multiplayer;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Scores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Pp;

namespace BanchoNET.Handlers.Stable.Controllers.OsuApi;

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
        [FromForm(Name = "sbk")] string? storyboardMD5 = null
    ) {
        if (string.IsNullOrEmpty(scoreDataB64) || replayFile == null)
            return Ok();
		
        var decryptedData = DecryptScoreData(osuVersion, ivB64, scoreDataB64, clientHashB64);
        if (decryptedData == null)
            return Ok();

        var scoreData = decryptedData.Value.scoreData;
        var clientHash = decryptedData.Value.clientHash;

        var beatmapMD5 = scoreData[0];
        var beatmap = await beatmapHandler.GetBeatmap(beatmapMD5);
        if (beatmap == null)
            return Ok("error: beatmap");

        var beatmapId = beatmap.Id;

        var username = scoreData[1];
        if (username[^1] == ' ')
            username = username[..^1];
        
        var player = await players.GetPlayerFromLogin(username, passwordMD5);
        if (player == null)
            return Ok();

        var score = new Score(scoreData[2..], beatmap, player);
		
        var uniqueIdMD5s = uniqueIds.Split('|', 2);
        var uninstallMd5 = uniqueIdMD5s[0].CreateMD5();
        var diskSignatureMd5 = uniqueIdMD5s[1].CreateMD5();

        try
        {
            var versionDate = DateTime.ParseExact(osuVersion[..8], "yyyyMMdd", null);
            
            //TODO this is broken
            if (versionDate != player.ClientDetails?.OsuVersion.Date)
                throw new Exception("osu! version mismatch");
			
            if (clientHash != player.ClientDetails.ClientHash)
                throw new Exception("Client hash mismatch");
			
            if (uninstallMd5 != player.ClientDetails.UninstallMD5)
                throw new Exception($"UniqueId1 mismatch ({uninstallMd5} != {player.ClientDetails.UninstallMD5})");

            if (diskSignatureMd5 != player.ClientDetails.DiskSignatureMD5)
                throw new Exception($"UniqueId2 mismatch ({diskSignatureMd5} != {player.ClientDetails.DiskSignatureMD5})");

            var serverScoreChecksum = score.ComputeOnlineChecksum(osuVersion, clientHash, storyboardMD5 ?? "");
            if (score.ClientChecksum != serverScoreChecksum)
                throw new Exception($"Online score checksum mismatch ({score.ClientChecksum} != {serverScoreChecksum})");

            if (beatmapMD5 != beatmapHash)
                throw new Exception($"Beatmap hash mismatch ({beatmapMD5} != {beatmapHash})");
        }
        catch (Exception e)
        {
            logger.LogError("Mismatching hashes on score submission", e);

            //await players.RestrictPlayer(player, "Mismatching hashes on score submission");
            //return Ok("error: ban");
        }

        if (score.Mode != player.Status.Mode)
        {
            player.Status.Mode = score.Mode;
            player.Status.CurrentMods = score.Mods;
			
            if (!player.IsRestricted)
            {
                playerService.EnqueueToPlayers(new ServerPackets()
                    .UserStats(player)
                    .FinalizeAndGetContent());
            }
        }
        
        if (score.Passed && replayFile.Length < 24 && !player.IsRestricted)
            await players.RestrictPlayer(player, "Submitted score with invalid replay length");

        score.TimeElapsed = score.Passed ? scoreTime : failTime;
        score.Player = player;
        
        var submission = await scoreService.Submit(ScoreSubmissionMapping.FromLegacy(score, beatmap));
        if (submission == null)
            return Ok("error: no");

        var prevStats = submission.PreviousStatistics.ToModeStats(submission.PreviousRank);
        
        ScoreSubmissionMapping.Apply(submission.Score, score);
        
        score.LeaderboardPosition = submission.Position;
        score.PreviousBest = submission.PreviousBest == null ? null : new Score(submission.PreviousBest)
        {
            LeaderboardPosition = submission.PreviousPosition
        };
        player.RecentScore = score;

        if (submission.IsNew && (AppSettings.CountFirstPlaceRanksByScore ? score.TotalScoreStatus : score.Status) == SubmissionStatus.Best)
        {
            if (beatmap.HasLeaderboard())
            {
                var scoreNotification = $"You achieved #{score.LeaderboardPosition} on a leaderboard!\n";

                if (AppSettings.DisplayScoreInNotification)
                    scoreNotification += $"({score.StandardisedTotalScore.SplitNumber()} score)\n";
                
                if (AppSettings.DisplayPPInNotification)
                    scoreNotification += $"({score.PP:F2}pp) ";

                //Subject to change
                if (AppSettings.DisplayPPInNotification
                    && (score.Misses > 0 || beatmap.MaxCombo - score.MaxCombo > 15))
                {
                    var fcPP = PpMethods.ComputeNoMissesScorePp(
                        beatmap,
                        score,
                        beatmap.MaxCombo
                    );
					
                    scoreNotification += $"[{fcPP:F2}pp if FC]";
                }
                
                player.Enqueue(new ServerPackets()
                    .Notification(scoreNotification)
                    .FinalizeAndGetContent());

                if (submission.IsFirstPlace)
                    AnnounceNewFirstScore(score, player, beatmap, submission.PreviousFirstPlace);
            }
        }
        
        if (submission.IsNew) await CaptureMultiplayerScore(player, score);

        if (score.Passed)
        {
            if (replayFile.Length >= 24)
            {
                var completed = await scoreService.Complete(score.Id, writeReplay: async path =>
                {
                    await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
                    await replayFile.CopyToAsync(fileStream);
                });
                score.HasReplay = completed.HasReplay;
            }
        }
        
        var previousBest = score.PreviousBest;
        await players.GetPlayerStats(player);
        var newStats = player.Stats[score.Mode];
        
        if (!player.IsRestricted)
        {
            playerService.EnqueueToPlayers(new ServerPackets()
                .UserStats(player)
                .FinalizeAndGetContent());
        }

        string response;
        var achievements = "";
        
        if (score.Passed && (int)score.Mode <= 3 && (int)score.Mode >= 0)
        {
            List<string> submissionCharts =
            [
                $"beatmapId:{beatmap.Id}",
                $"beatmapSetId:{beatmap.BeatmapsetId}",
                $"beatmapPlaycount:{(int)beatmap.Plays}",
                $"beatmapPasscount:{(int)beatmap.Passes}",
                $"approvedDate:{beatmap.LastUpdated:yyyy-MM-dd HH:mm:ss}",
                "\n",
                "chartId:beatmap",
                $"chartUrl:{beatmap.Set.Url()}",
                "chartName:Beatmap Ranking",
                ChartEntry("rank", previousBest?.LeaderboardPosition, score.LeaderboardPosition),
                ChartEntry("rankedScore", previousBest?.StandardisedTotalScore, score.StandardisedTotalScore),
                ChartEntry("totalScore", previousBest?.StandardisedTotalScore, score.StandardisedTotalScore),
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
                ChartEntry("rank", prevStats.Rank, newStats.Rank),
                ChartEntry("rankedScore", prevStats.RankedScore, newStats.RankedScore),
                ChartEntry("totalScore", prevStats.TotalScore, newStats.TotalScore),
                ChartEntry("maxCombo", prevStats.MaxCombo, newStats.MaxCombo),
                ChartEntry("accuracy",
                    MathF.Round(prevStats.Accuracy, 2),
                    MathF.Round(newStats.Accuracy, 2)),
                ChartEntry("pp", prevStats.PP, newStats.PP),
                $"achievements-new:{achievements}",
            ];

            if (beatmap.AwardsPp() && !player.IsRestricted)
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
    
    private void AnnounceNewFirstScore(
        Score score,
        Player player,
        Beatmap beatmap,
        ScoreDto? previousFirst
    ) {
        if (player.IsRestricted) return;
        
        var announceChannel = channels.GetChannel("#announce");
        if (announceChannel == null) return;
        
        var announcement = $"\x01ACTION achieved #1 on {beatmap.Embed()} with {score.Acc:F2}% and {score.PP}pp.";
        
        if (score.Mods > 0)
            announcement = announcement.Insert(0, $"+{score.Mods}");
        
        if (previousFirst != null && previousFirst.PlayerId != player.Id)
            announcement += $" (Previous #1: [https://{AppSettings.Domain}/u/{previousFirst.PlayerId} {previousFirst.Player.Username}])";
       
        channels.SendMessageTo(announceChannel, new Message
        {
            Sender = player.Username,
            Content = announcement,
            Destination = "",
            SenderId = player.Id
        });
    }

    /// <summary>
    /// Attaches a freshly submitted score to the multiplayer game it was played on, if any. The
    /// match is identified by the beatmap, so a client that submits minutes late still lands on
    /// the right scoreboard, and a solo score played on another map or after leaving the lobby
    /// is ignored.
    /// </summary>
    private async Task CaptureMultiplayerScore(
        Player player,
        Score score
    ) {
        if (player.Match is not { } match || string.IsNullOrEmpty(score.BeatmapMD5)) return;

        var gameId = match.ClaimGameFor(score.BeatmapMD5, player.Id);
        if (gameId == null) return;

        try
        {
            await matchHistory.AppendScore(new MultiplayerScoreDto
            {
                GameId = gameId.Value,
                PlayerId = player.Id,
                ScoreId = score.Id,
                Team = match.GetPlayerSlot(player.Id)?.Team ?? LobbyTeams.Neutral,
                TotalScore = score.TotalScore,
                MaxCombo = score.MaxCombo,
                Accuracy = score.Acc,
                Grade = score.Grade,
                Mods = score.Mods,
                Count300 = score.Count300,
                Count100 = score.Count100,
                Count50 = score.Count50,
                Gekis = score.Gekis,
                Katus = score.Katus,
                Misses = score.Misses,
                Failed = !score.Passed
            });
        }
        catch (Exception ex)
        {
            // Missing a scoreboard entry is not a reason to fail the player's submission
            logger.LogError($"Failed to attach score {score.Id} to multiplayer game {gameId}", ex);
        }
    }

    private static (string[] scoreData, string clientHash)? DecryptScoreData(
        string osuVersion,
        string ivB64,
        string scoreDataB64,
        byte[] clientHashB64
    ) {
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
        byte[] bytesToDecipher
    ) {
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