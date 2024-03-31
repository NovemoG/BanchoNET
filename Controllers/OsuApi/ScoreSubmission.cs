using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;
using AkatsukiPp;
using BanchoNET.Objects;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
	[HttpPost("/web/osu-submit-modular-selector.php")]
	public async Task<IActionResult> SubmitScore(
		[FromForm(Name = "ft")] int failTime,
		[FromForm(Name = "bmk")] string beatmapHash,
		[FromForm(Name = "iv")] string ivB64,
		[FromForm(Name = "c1")] string uniqueIds,
		[FromForm(Name = "st")] int scoreTime,
		[FromForm(Name = "pass")] string pwdMD5,
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
		var beatmap = await _bancho.GetBeatmap(beatmapMD5: beatmapMD5);
		if (beatmap == null)
			return Ok("error: beatmap");

		var username = scoreData[1];
		if (username[^1] == ' ')
			username = username[..^1];

		var player = await _bancho.GetPlayerFromLogin(username, pwdMD5);
		if (player == null)
			return Ok();

		var score = ParseScoreData(scoreData[2..], beatmap, player);
		
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
			
			//TODO restrict
			return Ok("error: ban");
		}

		await _bancho.UpdateLatestActivity(player);

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

		if (await _bancho.GetScore(checksum: score.ClientChecksum) != null)
		{
			Console.WriteLine($"[ScoreSubmission] {player.Username} tried to submit a duplicate score.");
			return Ok("error: no");
		}
		
		score.CalculateAccuracy();

		if (await _bancho.EnsureLocalBeatmapFile(beatmap.MapId, beatmapMD5))
		{
			score.CalculatePerformance(beatmap.MapId);

			if (score.Passed)
			{
				var prevBest = await _bancho.GetPlayerBestScore(player, beatmapMD5, score.Mode);
				
				score.ComputeSubmissionStatus(prevBest, _config.SubmitByPP);

				if (beatmap.Status != BeatmapStatus.LatestPending)
					await _bancho.SetScoreLeaderboardPosition(beatmap, score);
			}
			else
				score.Status = SubmissionStatus.Failed;
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
				var scoreNotification = $"You achieved #{score.LeaderboardPosition}!";

				if (_config.DisplayPPOnLeaderboard)
					scoreNotification += $" ({score.PP}pp)";

				if (_config.DisplayScoreOnLeaderboard)
					scoreNotification += $"\n({score.TotalScore} score)";
				
				if (_config.DisplayMissesOnLeaderboard)
					scoreNotification += $"\n({score.Misses} misses)";

				//Subject to change
				if (score.Misses > 0 || beatmap.MaxCombo - score.MaxCombo > 15)
				{
					var fcPP = AkatsukiPpMethods.ComputeNoMissesScorePp(
						Storage.GetBeatmapPath(beatmap.MapId),
						score,
						beatmap.MaxCombo);
					
					scoreNotification += $"\n({fcPP}pp if FC)";
				}
				
				Console.WriteLine($"[ScoreSubmission] Sending: {scoreNotification}");
				
				using var notification = new ServerPackets();
				notification.Notification(scoreNotification);
				player.Enqueue(notification.GetContent());
				
				if (score.LeaderboardPosition == 1 && !player.Restricted)
				{
					var announceChannel = _session.GetChannel("#announce");

					var announcement = $@"\x01ACTION achieved #1 on {beatmap.Embed} with {score.Acc:2F}% and {score.PP}pp.";
					
					//TODO if 1st on lb announce
				}
			}

			await _bancho.UpdatePlayerBestScoreOnMap(beatmap, score);
		}

		await _bancho.InsertScore(score);

		if (score.Passed)
		{
			if (replayFile.Length >= 24)
				await replayFile.CopyToAsync(new FileStream(Storage.GetReplayPath(score.Id!.Value), FileMode.Create));
			else
			{
				Console.WriteLine($"[ScoreSubmission] {player.Username} submitted a replay file with invalid length.");

				if (player is { Restricted: false, Online: true })
				{
					//TODO restrict
					
					_session.LogoutPlayer(player);
				}
			}
		}

		var stats = player.Stats[player.Status.Mode];
		var prevStats = stats.Copy();

		stats.PlayTime += (int)MathF.Floor(score.TimeElapsed / 1000f);
		stats.PlayCount += 1;
		stats.TotalScore += score.TotalScore;
		stats.UpdateHits(score);
		
		//TODO check if this works with mods
		var previousScore = score.PreviousBest;
		if (score.Passed && beatmap.HasLeaderboard() && beatmap.Status != BeatmapStatus.Qualified)
		{
			if (score.MaxCombo > stats.MaxCombo)
				stats.MaxCombo = score.MaxCombo;
			
			if (beatmap.AwardsPP() && score.Status == SubmissionStatus.Best)
			{
				var additionalRankedScore = score.TotalScore;
				if (previousScore != null)
				{
					additionalRankedScore -= previousScore.TotalScore;

					if (score.Grade != previousScore.Grade)
					{
						if (score.Grade >= Grade.A)
							stats.Grades[score.Grade] += 1;

						if (previousScore.Grade >= Grade.A)
							stats.Grades[score.Grade] -= 1;
					}
				}
				else
					if (score.Grade >= Grade.A)
						stats.Grades[score.Grade] += 1;

				stats.RankedScore += additionalRankedScore;
				
				
				//TODO Fetch top 100 scores, update pp and acc
				//TODO fetch best scores count to update bonus pp
				//TODO update player rank
			}
		}
		
		//TODO update stats in database

		if (!player.Restricted)
		{
			using var statsPacket = new ServerPackets();
			statsPacket.UserStats(player);
			_session.EnqueueToPlayers(statsPacket.GetContent());

			beatmap.Plays += 1;
			if (score.Passed)
				beatmap.Passes += 1;
			
			//TODO update beatmap stats in database
		}
		
		//TODO update recent player score
		
		var response = "";
		var achievements = "";
		
		if (!score.Passed || (int)score.Mode > 3 || (int)score.Mode < 0)
			response = "error: no";
		else
		{
			if (beatmap.AwardsPP() && !player.Restricted)
			{
				//var unlockedAchievements = new List<Achievement>();
				//TODO server achievements
				//TODO fetch player achievements
				//TODO achievements string
			}

			if (previousScore != null)
			{
				//TODO beatmaprankikngcharts
			}
			else
			{
				//TODO beatmaprankikngcharts
			}
			
			//TODO overallrankingcharts
			
			//TODO write response
		}
		
		return Ok(response);
	}

	private static Score ParseScoreData(string[] scoreData, Beatmap beatmap, Player player)
	{
		var mods = (Mods)int.Parse(scoreData[11]);
		Enum.TryParse(scoreData[10], out Grade grade);
		
		return new Score
		{
			Beatmap = beatmap,
			BeatmapMD5 = beatmap.MD5,
			Player = player,
			PlayerId = player.Id,
			
			ClientChecksum = scoreData[0],
			Count300 = int.Parse(scoreData[1]),
			Count100 = int.Parse(scoreData[2]),
			Count50 = int.Parse(scoreData[3]),
			Gekis = int.Parse(scoreData[4]),
			Katus = int.Parse(scoreData[5]),
			Misses = int.Parse(scoreData[6]),
			TotalScore = int.Parse(scoreData[7]),
			MaxCombo = int.Parse(scoreData[8]),
			Perfect = scoreData[9] == "True",
			Grade = grade,
			Mods = mods,
			Passed = scoreData[12] == "True",
			Mode = ((GameMode)int.Parse(scoreData[13])).FromMods(mods),
			ClientTime = DateTime.ParseExact(scoreData[14], "yyMMddHHmmss", null),
			ClientFlags = (ClientFlags)int.Parse(scoreData[15]),
			ServerTime = DateTime.UtcNow
		};
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
}