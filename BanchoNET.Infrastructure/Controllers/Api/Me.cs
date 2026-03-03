using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    private static readonly Dictionary<string, GameMode> ModeMap = new()
    {
        { "osu", GameMode.VanillaStd },
        { "taiko", GameMode.VanillaTaiko },
        { "mania", GameMode.VanillaMania },
        { "fruits", GameMode.VanillaCatch }
    };
    
    [HttpGet("me")]
    public async Task<ActionResult<ApiPlayer?>> GetMe() {
        var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(sub, out var uid)) return Unauthorized();

        var userInfo = await players.GetPlayerInfo(uid);
        if (userInfo == null) return NotFound();
        
        var country = userInfo.Country.ParseCountry();
        var mainMode = "osu"; //TODO
        var mode = ModeMap[mainMode];
        var rank = await players.GetPlayerGlobalRank(mode, uid); //TODO get all
        var countryRank = await players.GetPlayerCountryRank(mode, userInfo.Country, uid);
        var stats = await players.GetPlayerModeStats(uid, (byte)mode); //TODO get all
        var peakRank = await histories.GetPeakRank(uid, (byte)mode) ?? new PeakRank();
        var playcountHistory = await histories.GetPlayCountHistory(uid, (byte)mode);
        var replaysHistory = await histories.GetReplaysHistory(uid, (byte)mode);
        var rankHistory = await histories.GetRankHistory(uid, (byte)mode);
        
        var osuStats = new Statistics {
            Count100 = stats!.Total100s,
            Count300 = stats.Total300s,
            Count50 = stats.Total50s,
            CountMiss = 0, //TODO store misses :sob:
            GlobalRank = rank,
            GlobalRankPercent = 0.0000001d, //TODO
            Pp = stats.PP,
            RankedScore = stats.RankedScore,
            HitAccuracy = stats.Accuracy,
            Accuracy = stats.Accuracy / 100f,
            PlayCount = stats.PlayCount,
            PlayTime = stats.PlayTime,
            TotalScore = stats.TotalScore,
            MaximumCombo = stats.MaxCombo,
            ReplaysWatchedByOthers = stats.ReplayViews,
            IsRanked = true, //TODO
            GradeCounts = new GradeCounts {
                Ss = stats.XCount,
                Ssh = stats.XHCount,
                S = stats.SCount,
                Sh = stats.SHCount,
                A = stats.ACount
            },
            CountryRank = countryRank,
            Rank = new Rank{ Country = countryRank }
        };
        
        var apiPlayer = new ApiPlayer
        {
            SessionVerified = true,
            CountryCode = country.Code,
            ID = uid,
            IsActive = !userInfo.Inactive,
            IsBot = false,
            IsDeleted = false, //TODO
            IsOnline = true, //TODO if hideOnlineActivity then return false
            IsSupporter = userInfo.RemainingSupporter > DateTime.UtcNow,
            SupportLevel = 1, //TODO
            LastVisit = userInfo.LastActivityTime, //TODO if hideOnlineActivity then return null
            PmFriendsOnly = true, //TODO why is this not in db?
            Username = userInfo.Username,
            HasSupported = true, //TODO
            JoinDate = userInfo.CreationTime,
            Playmode = mainMode,
            Country = country,
            IsRestricted = (userInfo.Privileges & 1) == 0,
            MonthlyPlaycounts = new MonthlyPlaycounts[playcountHistory.Count],
            ReplaysWatchedCounts = new ReplaysWatchedCounts[replaysHistory.Count],
            RankHighest = new RankHighest{
                Rank = peakRank.Value,
                UpdatedAt = peakRank.Date
            },
            ScoresBestCount = 0, //TODO
            Statistics = osuStats,
            StatisticsRulesets = new StatisticsRulesets {
                Osu = osuStats //TODO add rest
            },
            //TODO team
            //TODO achievements
        };
        
        apiPlayer.DailyChallengeUserStats.UserID = uid;
        apiPlayer.MatchmakingStats[0].UserID = uid;
        apiPlayer.MatchmakingStats[0].Rank = rank;
        
        for (var i = playcountHistory.Count - 1; i >= 0; i--)
        {
            apiPlayer.MonthlyPlaycounts[i] = new MonthlyPlaycounts
            {
                StartDate = userInfo.CreationTime.AddMonths(-(i - playcountHistory.Count + 1)),
                Count = playcountHistory[i]
            };
        }

        for (var i = replaysHistory.Count - 1; i >= 0; i--)
        {
            apiPlayer.ReplaysWatchedCounts[i] = new ReplaysWatchedCounts
            {
                StartDate = userInfo.CreationTime.AddMonths(-(i - replaysHistory.Count + 1)),
                Count = replaysHistory[i]
            };
        }

        apiPlayer.RankHistory.Data = new int[rankHistory.Count];
        for (var i = rankHistory.Count - 1; i >= 0; i--)
        {
            apiPlayer.RankHistory.Data[i] = rankHistory[i];
        }
        
        return new JsonResult(apiPlayer, SnakeCaseNamingPolicy.Options); //TODO
    }
}