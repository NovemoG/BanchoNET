﻿using System.Web;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
	[HttpGet("/web/osu-osz2-getscores.php")]
	public async Task<IActionResult> GetScores(
		[FromQuery(Name = "us")] string username,
		[FromQuery(Name = "ha")] string passwordMD5,
		[FromQuery(Name = "s")] string? fromEditorValue,
		[FromQuery(Name = "v")] int leaderboardType,
		[FromQuery(Name = "c")] string mapMD5,
		[FromQuery(Name = "f")] string? mapFilename,
		[FromQuery(Name = "m")] int modeValue,
		[FromQuery(Name = "i")] int setId,
		[FromQuery(Name = "mods")] int modsValue,
		[FromQuery(Name = "h")] string? mapPackageHash,
		[FromQuery(Name = "a")] string? aqnFilesFoundValue)
	{
		var fromEditor = fromEditorValue == "1";
		var aqnFilesFound = aqnFilesFoundValue == "1";
		
		username = HttpUtility.UrlDecode(username);
		var player = await _bancho.GetPlayerFromLogin(username, passwordMD5);
		
		if (player == null)
			return Unauthorized("auth fail");

		if (_session.BeatmapNeedsUpdate(mapMD5))
			return Responses.BytesContentResult("1|false");
		
		if (_session.IsBeatmapNotSubmitted(mapMD5))
			return Responses.BytesContentResult("-1|false");

		var mods = (Mods)modsValue;
		if (mods.HasMod(Mods.Relax))
		{
			if (modeValue == (int)GameMode.VanillaMania)
				mods &= ~Mods.Relax;
			else
				modeValue += 4;
		}
		else if (mods.HasMod(Mods.Autopilot))
		{
			if (modeValue is (int)GameMode.VanillaTaiko or (int)GameMode.VanillaCatch or (int)GameMode.VanillaMania)
				mods &= ~Mods.Autopilot;
			else
				modeValue += 8;
		}

		var mode = (GameMode)modeValue;
		if (mode != player.Status.Mode)
		{
			player.Status.Mode = mode;
			player.Status.CurrentMods = mods;

			if (!player.Restricted)
			{
				using var statsPacket = new ServerPackets();
				statsPacket.UserStats(player);
				_session.EnqueueToPlayers(statsPacket.GetContent());
			}
		}
		
		var beatmap = await _bancho.GetBeatmap(beatmapMD5: mapMD5, setId: setId);
		if (beatmap == null)
		{
			if (await _bancho.CheckIfMapExistsOnBanchoByFilename(mapFilename))
			{
				_session.CacheNeedUpdateBeatmap(mapMD5);
				return Ok("1|false");
			}
			_session.CacheNotSubmittedBeatmap(mapMD5);
			return Ok("-1|false");
		}

		if (beatmap.Status < BeatmapStatus.Ranked)
			return Responses.BytesContentResult($"{(int)beatmap.Status}|false");
		
		(List<ScoreDto> Scores, Score? PlayerBest) leaderboard = !fromEditor
			? await GetLeaderboardScores(leaderboardType, beatmap, mode, mods, player)
			: ([], null);
		
		//TODO fetch rating
		var rating = 0.0f;

		string response;
		var responseLines = new List<string>
		{
			$"{(int)beatmap.Status}|false|{beatmap.MapId}|{beatmap.SetId}|{leaderboard.Scores.Count}|0|",
			$"0\n{beatmap.FullName()}\n{rating}"
		};

		if (leaderboard.Scores.Count == 0)
		{
			responseLines.AddRange(["", ""]);
			response = string.Join("\n", responseLines);

			return Responses.BytesContentResult(response);
		}

		responseLines.Add(leaderboard.PlayerBest != null ? FormatBestScore(leaderboard.PlayerBest, player) : "");
		responseLines.AddRange(leaderboard.Scores.Select((score, i) => FormatScore(score, i + 1)));
		
		response = string.Join("\n", responseLines);
		
		return Responses.BytesContentResult(response);
	}
	
	private async Task<(List<ScoreDto>, Score?)> GetLeaderboardScores(
		int leaderboardType,
		Beatmap beatmap,
		GameMode mode,
		Mods mods,
		Player player)
	{
		var type = (LeaderboardType)leaderboardType;
		var scores = await _bancho.GetBeatmapLeaderboard(beatmap.MD5, mode, type, mods, player);
		
		Score? playerBest = null;
		if (scores.Count > 0)
		{
			var withMods = type is LeaderboardType.Mods or LeaderboardType.CountryMods or LeaderboardType.FriendsMods;
			playerBest = withMods
				? await _bancho.GetPlayerBestScoreWithModsOnMap(player, beatmap.MD5, mode, mods)
				: await _bancho.GetPlayerBestScoreOnMap(player, beatmap.MD5, mode);
			
			if (playerBest != null)
				await _bancho.SetScoreLeaderboardPosition(beatmap, playerBest, withMods, mods);
		}
		
		return (scores, playerBest);
	}

	private static string FormatScore(ScoreDto dto, int position)
	{
		var scoreAsPp = dto.Mode >= (byte)GameMode.RelaxStd || AppSettings.SortLeaderboardByPP;
		return $"{(int)dto.Id}|{dto.Player.Username}|{(int)(scoreAsPp ? dto.PP : dto.Score)}|{dto.MaxCombo}|{dto.Count50}|{dto.Count100}|{dto.Count300}|{dto.Misses}|{dto.Katus}|{dto.Gekis}|{dto.Perfect}|{dto.Mods}|{dto.PlayerId}|{position}|{DateTimeToUnix(dto.PlayTime)}|1";	//TODO this '1' tells client whether score has a saved replay
	}

	private static string FormatBestScore(Score score, Player player)
	{
		var scoreAsPp = score.Mode >= GameMode.RelaxStd || AppSettings.SortLeaderboardByPP;
		return $"{(int)score.Id!}|{player.Username}|{(int)(scoreAsPp ? score.PP : score.TotalScore)}|{score.MaxCombo}|{score.Count50}|{score.Count100}|{score.Count300}|{score.Misses}|{score.Katus}|{score.Gekis}|{score.Perfect}|{(int)score.Mods}|{player.Id}|{score.LeaderboardPosition}|{DateTimeToUnix(score.ClientTime)}|1"; //TODO this '1' tells client whether score has a saved replay
	}

	private static long DateTimeToUnix(DateTime dateTime)
	{
		return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
	}
}