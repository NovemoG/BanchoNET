using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;
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
			//if (osuVersion != player.ClientDetails.OsuVersion)
			//	throw new Exception("ovu! version mismatch");
			
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
			Console.Write($"[ScoreSubmission] {e.Message}");
			
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
			//Duplicated score
			return Ok("error: no");
		}
		
		score.CalculateAccuracy();

		//TODO score pp
		score.ComputeSubmissionStatus(await _bancho.GetPlayerBestScore(player, beatmapMD5, score.Mode), _config.SubmitByPP);
		//TODO place on leaderboard

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

				using var notification = new ServerPackets();
				notification.Notification(scoreNotification);
				player.Enqueue(notification.GetContent());
				
				//TODO if 1st on lb announce
			}
			
			//TODO update previous personal best to be submitted
		}

		await _bancho.InsertScore(score);

		if (score.Passed)
		{
			//TODO parse replay file data
		}

		var stats = player.Stats[player.Status.Mode];
		var prevStats = stats.Copy();

		stats.PlayTime += (int)MathF.Floor(score.TimeElapsed / 1000f);
		stats.PlayCount += 1;
		stats.TotalScore += score.TotalScore;
		stats.UpdateHits(score);
		
		var previousScore = score.PreviousBest;
		if (score.Passed && beatmap.HasLeaderboard())
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
				{
					if (score.Grade >= Grade.A)
						stats.Grades[score.Grade] += 1;
				}

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
		
		return new Score
		{
			Beatmap = beatmap,
			Player = player,
			
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
			Grade = (Grade)int.Parse(scoreData[10]),
			Mods = mods,
			Passed = scoreData[12] == "True",
			Mode = ((GameMode)int.Parse(scoreData[13])).FromMods(mods),
			ClientTime = DateTime.ParseExact(scoreData[14], "yyyyMMddHHmmss", null),
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
		
		/*cipher.Init(false, keyParamWithIv);
		var comparisonBytes = new byte[cipher.GetOutputSize(scoreB64.Length)];
		var length = cipher.ProcessBytes(scoreB64, comparisonBytes, 0);
		cipher.DoFinal(comparisonBytes, length);

		var scoreData = Encoding.UTF8.GetString(comparisonBytes).Split(':');*/
		
		/*cipher.Init(false, keyParamWithIv);
		comparisonBytes = new byte[cipher.GetOutputSize(clientHashB64.Length)];
		length = cipher.ProcessBytes(clientHashB64, comparisonBytes, 0);
		cipher.DoFinal(comparisonBytes, length);
		
		var clientHash = Encoding.UTF8.GetString(comparisonBytes);*/
		
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

		return Encoding.UTF8.GetString(comparisonBytes);
	}
}