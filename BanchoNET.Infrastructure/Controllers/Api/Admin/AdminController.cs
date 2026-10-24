using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Admin;

[Route("api/v2/admin")]
public class AdminController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IServerStatsService serverStats,
    IBeatmapsetCrawler crawler,
    IOsuSessionCookieService cookies
) : ApiControllerBase(players, playerService, beatmaps)
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats() {
        if (await GetOwnerId() is null) return NotFound();

        return JsonSnake(new
        {
            OnlineUsers = await serverStats.GetOnlineCount(),
            RegisteredUsers = await Players.TotalPlayerCount(countRestricted: true),
        });
    }

    [HttpGet("crawler")]
    public async Task<IActionResult> GetCrawlerStatus() {
        if (await GetOwnerId() is null) return NotFound();

        return JsonSnake(crawler.Status);
    }

    [HttpGet("session-cookie")]
    public async Task<IActionResult> GetSessionCookie() {
        if (await GetOwnerId() is null) return NotFound();

        return JsonSnake(CookieResponse());
    }

    [HttpGet("session-cookie/reveal")]
    public async Task<IActionResult> RevealSessionCookie() {
        if (await GetOwnerId() is null) return NotFound();

        var cookie = OsuSessionCookie.Current;
        return JsonSnake(new { Value = OsuSessionCookie.Format(cookie.Value, cookie.ExpiresAt) });
    }

    [HttpPost("session-cookie")]
    public async Task<IActionResult> UpdateSessionCookie(
        [FromForm] string? value
    ) {
        if (await GetOwnerId() is null) return NotFound();

        try
        {
            await cookies.Update(value);
        }
        catch (FormatException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        return JsonSnake(CookieResponse());
    }

    [HttpDelete("session-cookie")]
    public async Task<IActionResult> ClearSessionCookie() {
        if (await GetOwnerId() is null) return NotFound();

        await cookies.Clear();

        return JsonSnake(CookieResponse());
    }

    private async Task<int?> GetOwnerId() {
        if (!User.TryGetUserId(out var uid)) return null;

        var player = await Players.GetPlayerOrOffline(uid);
        if (player == null || !player.Privileges.HasPrivilegeOrHigher(PlayerPrivileges.Owner)) return null;

        return uid;
    }

    private static object CookieResponse() {
        var note = OsuSessionCookie.GetExpiryNote();

        return new
        {
            HasValue = OsuSessionCookie.Current.Value.Length > 0,
            Note = note?.Text,
            NoteWarning = note?.Warning ?? false,
        };
    }
}