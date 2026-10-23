using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Handlers.Stable.Controllers.OsuApi;

public partial class OsuController
{
	[HttpGet("osu-osz2-getscores.php")]
	public async Task<IActionResult> GetScores(
		[FromQuery(Name = "us")] string username,
		[FromQuery(Name = "ha")] string passwordMD5,
		[FromQuery(Name = "v")] int leaderboardType,
		[FromQuery(Name = "c")] string mapMD5,
		[FromQuery(Name = "f")] string mapFilename,
		[FromQuery(Name = "m")] int modeValue,
		[FromQuery(Name = "i")] int setId,
		[FromQuery(Name = "mods")] int modsValue,
		[FromQuery(Name = "h")] string? mapPackageHash,
		[FromQuery(Name = "a")] string? aqnFilesFoundValue,
		[FromQuery(Name = "s")] string? fromEditorValue = null
	) {
		var fromEditor = fromEditorValue == "1";
		var aqnFilesFound = aqnFilesFoundValue == "1";
		
		var player = await players.GetPlayerFromLogin(username, passwordMD5);
		if (player == null)
			return Unauthorized("auth fail");

		if (beatmapService.BeatmapNeedsUpdate(mapMD5))
			return Responses.BytesContentResult("1|false");
		
		var beatmap = await beatmapHandler.GetBeatmap(beatmapMD5: mapMD5, setId: setId);
		if (beatmap == null)
		{
			if (await beatmapHandler.CheckIfMapExistsOnBanchoByFilename(mapFilename))
				return Ok("1|false");
			
			//any string can be provided through this endpoint so we shouldn't cache any
			return Ok("-1|false");
		}
		
		var mods = (LegacyMods)modsValue;
		var mode = (GameMode)modeValue;
		if (mode != player.Status.Mode)
		{
			player.Status.Mode = mode;
			player.Status.CurrentMods = mods;

			if (!player.IsRestricted)
			{
				playerService.EnqueueToPlayers(new ServerPackets()
					.UserStats(player)
					.FinalizeAndGetContent());
			}
		}

		if (beatmap.Status < BeatmapStatus.Ranked)
			return Responses.BytesContentResult("0|false");
		
        var (pp, leaderboardSort) = await scores.GetLeaderboardPreferences(player.Id);
        var leaderboard = !fromEditor
            ? await scores.GetLeaderboardScores(
	            beatmap.Id,
	            mode,
	            (LeaderboardType)leaderboardType,
                mods.ToKeysString(),
	            player.Id,
	            player.Geoloc.Country.Acronym,
                player.Friends.ToHashSet(),
                [],
	            pp,
	            leaderboardSort, AppSettings.ScoresOnLeaderboard)
            : new ScoreLeaderboard([], 0, null, 0);

        string response;

		var rating = beatmap.Set.Rating;
		var responseLines = new List<string>
		{
			$"{beatmap.Status.ToLegacyStatus()}|false|{beatmap.Id}|{beatmap.BeatmapsetId}|{leaderboard.Scores.Count}|0|",
			$"0\n{beatmap.FullName()}\n{rating}"
		};

		if (leaderboard.Scores.Count == 0)
		{
			responseLines.AddRange(["", ""]);
			response = string.Join("\n", responseLines);

			return Responses.BytesContentResult(response);
		}

		responseLines.Add(leaderboard.UserScore != null ? FormatScore(leaderboard.UserScore, leaderboard.Position, pp) : "");
		responseLines.AddRange(leaderboard.Scores.Select((score, i) => FormatScore(score, i + 1, pp)));
		
		response = string.Join("\n", responseLines);
		
		return Responses.BytesContentResult(response);
	}

	private static string FormatScore(ScoreDto dto, int position, bool scoreAsPp)
	{
		return $"{(int)dto.Id}|{dto.Player.Username}|{(int)(scoreAsPp ? MathF.Round(dto.PP) : dto.TotalScore)}|{dto.MaxCombo}|{dto.GetCount50()}|{dto.GetCount100()}|{dto.GetCount300()}|{dto.GetCountMiss()}|{dto.GetCountKatu()}|{dto.GetCountGeki()}|{dto.LegacyPerfect}|{(int)dto.Mods}|{dto.PlayerId}|{position}|{dto.PlayTime.ToUnixTimeSeconds()}|{dto.HasReplay}";
	}

	private static long DateTimeToUnix(DateTime dateTime)
	{
		return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
	}
}