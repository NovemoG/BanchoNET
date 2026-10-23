using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    public record LeaderboardPreferences(
        [property: JsonPropertyName("performance_points")]
        bool PerformancePoints,
        LeaderboardSort Sorting
    );

    [HttpGet("leaderboard")]
    [RequireScope(OAuthScopes.Identify)]
    public async Task<IActionResult> GetLeaderboardPreferences(
        [FromServices] BanchoDbContext db
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var preferences = await db.Players.Where(p => p.Id == uid)
            .Select(p => new LeaderboardPreferences(p.LeaderboardPerformancePoints, p.LeaderboardSorting))
            .SingleOrDefaultAsync();
        
        return preferences == null ? NotFound() : JsonSnake(preferences);
    }

    [HttpPut("leaderboard")]
    [RequireScope(OAuthScopes.Identify)]
    public async Task<IActionResult> UpdateLeaderboardPreferences(
        [FromBody] LeaderboardPreferences preferences,
        [FromServices] BanchoDbContext db
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        if (!Enum.IsDefined(preferences.Sorting) || preferences.Sorting == LeaderboardSort.PP)
            return BadRequest();
        
        var updated = await db.Players.Where(p => p.Id == uid).ExecuteUpdateAsync(p => p
            .SetProperty(v => v.LeaderboardPerformancePoints, preferences.PerformancePoints)
            .SetProperty(v => v.LeaderboardSorting, preferences.Sorting));
        
        return updated == 0 ? NotFound() : Ok();
    }
}