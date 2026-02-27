using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
    [HttpPost("osu-error.php")]
    public async Task<IActionResult> OsuError(
        [FromForm(Name = "u")] string username,
        [FromForm(Name = "p")] string passwordMD5,
        [FromForm(Name = "i")] int userId,
        [FromForm(Name = "beatmap_count")] int beatmapCount,
        [FromForm(Name = "beatmap_checksum")] string beatmapMD5,
        [FromForm(Name = "beatmap_id")] int beatmapId,
        [FromForm(Name = "version")] string osuVersion,
        [FromForm(Name = "ram")] int ramUsed,
        [FromForm] string osuMode,
        [FromForm] string gameMode,
        [FromForm] int gameTime,
        [FromForm] int audioTime,
        [FromForm] string culture,
        [FromForm] string exception,
        [FromForm] string? feedback,
        [FromForm] string stacktrace,
        [FromForm] bool soft,
        [FromForm] int compatibility,
        [FromForm] string config)
    {
        if (!AppSettings.Debug)
            return Ok("");
        
        User? player;
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(passwordMD5))
        {
            player = await players.GetPlayerFromLogin(username, passwordMD5);
            
            if (player == null)
                Console.WriteLine($"[OsuError] Failed to find player {username} with password {passwordMD5}");
        }
        else player = null;
        
        Console.WriteLine($"[OsuError] {player?.Username ?? "Offline User"} sent an error with description: {feedback} ({exception})");
        Console.WriteLine($"Stacktrace: {stacktrace[..^2]}");
        
        return Ok("");
    }
}