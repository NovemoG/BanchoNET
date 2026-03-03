using System.ComponentModel;
using System.Text.Json.Serialization;
using BanchoNET.Core.Utils.Json;

namespace BanchoNET.Core.Models.Players;

public class ApiPlayer
{
    public string AvatarUrl => $"https://a.ppy.sh/{ID}.jpg";
    public string CountryCode { get; set; } = "Unknown";
    public string DefaultGroup { get; set; } = "default"; //TODO
    public int ID { get; set; }
    public bool IsActive { get; set; }
    public bool IsBot { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsOnline { get; set; }
    public bool IsSupporter { get; set; }
    public DateTime? LastVisit { get; set; }
    public bool PmFriendsOnly { get; set; }
    public string? ProfileColour { get; set; } //TODO
    public string Username { get; set; } = null!;
    public string? CoverUrl { get; set; } //TODO this probably should have default value like AvatarUrl
    public string? Discord { get; set; }
    public bool HasSupported { get; set; }
    public string? Interests { get; set; }
    public DateTime JoinDate { get; set; }
    public string? Location { get; set; }
    public int MaxBlocks { get; set; } = 200;
    public int MaxFriends { get; set; } = 1000;
    public string? Occupation { get; set; }
    public string Playmode { get; set; } = "osu"; //TODO main mode
    public string[] Playstyle { get; set; } = [];
    public int PostCount { get; set; }
    public int ProfileHue { get; set; }
    public string[] ProfileOrder { get; set; } = [ //TODO
        "me",
        "top_ranks",
        "historical",
        "recent_activity",
        "beatmaps",
        "medals",
        "kudosu"
    ];
    public string? Title { get; set; }
    public string? TitleUrl { get; set; }
    public string? Twitter { get; set; }
    public string? Website { get; set; }
    public Country Country { get; set; } = null!;
    public Cover Cover { get; set; } = new(); //TODO
    public bool IsRestricted { get; set; }
    public Kudosu Kudosu { get; set; } = new();
    public object[] AccountHistory { get; set; } = []; //TODO
    public object? ActiveTournamentBanner { get; set; } //TODO
    public object[] ActiveTournamentBanners { get; set; } = []; //TODO
    public object[] Badges { get; set; } = []; //TODO
    public int BeatmapPlaycountsCount { get; set; }
    public int CommentsCount { get; set; }
    public object? CurrentSeasonStats { get; set; } //TODO
    public DailyChallengeUserStats DailyChallengeUserStats { get; set; } = new();
    public int FavouriteBeatmapsetCount { get; set; }
    public int FollowerCount { get; set; }
    public int GraveyardBeatmapsetCount { get; set; }
    public object[] Groups { get; set; } = []; //TODO
    public int GuestBeatmapsetCount { get; set; }
    public int LovedBeatmapsetCount { get; set; }
    public int MappingFollowerCount { get; set; }
    public MatchmakingStats[] MatchmakingStats { get; set; } = [new()];
    public MonthlyPlaycounts[] MonthlyPlaycounts { get; set; } = [];
    public int NominatedBeatmapsetCount { get; set; }
    public Page Page { get; set; } = new();
    public int PendingBeatmapsetCount { get; set; }
    public string[] PreviousUsernames { get; set; } = [];
    public RankHighest RankHighest { get; set; } = new();
    public int RankedBeatmapsetCount { get; set; }
    public ReplaysWatchedCounts[] ReplaysWatchedCounts { get; set; } = [];
    public int ScoresBestCount { get; set; }
    public int ScoresFirstCount { get; set; }
    public int ScoresPinnedCount { get; set; }
    public int ScoresRecentCount { get; set; }
    public object? SessionVerificationMethod { get; set; } //TODO
    public bool SessionVerified { get; set; }
    public Statistics Statistics { get; set; } = new(); //TODO statistics for mode set in playmode
    public StatisticsRulesets StatisticsRulesets { get; set; } = new();
    public int SupportLevel { get; set; }
    public Team? Team { get; set; }
    public UserAchievements[] UserAchievements { get; set; } = [];
    public RankHistory RankHistory { get; set; } = new();
    public int RankedAndApprovedBeatmapsetCount { get; set; }
    public int UnrankedBeatmapsetCount { get; set; }
}

public class Country
{
    public string Code { get; set; } = "Unknown";
    public string Name { get; set; } = "Unknown";
}

public class Cover
{
    public string? CustomUrl { get; set; }
    public string? Url { get; set; }
    public string? ID { get; set; }
}

public class Kudosu
{
    public int Available { get; set; }
    public int Total { get; set; }
}

public class DailyChallengeUserStats
{
    public int DailyStreakBest { get; set; }
    public int DailyStreakCurrent { get; set; }
    public DateTime LastUpdate { get; set; }
    public DateTime LastWeeklyStreak { get; set; }
    public int Playcount { get; set; }
    public int Top10PPlacements { get; set; }
    public int Top50PPlacements { get; set; }
    public int UserID { get; set; }
    public int WeeklyStreakBest { get; set; }
    public int WeeklyStreakCurrent { get; set; }
}

public class MatchmakingStats //TODO
{
    public int FirstPlacements { get; set; }
    public bool IsRatingProvisional { get; set; } = true;
    public int Plays { get; set; }
    public int PoolID { get; set; } = 1;
    public int Rank { get; set; }
    public int Rating { get; set; }
    public int TotalPoints { get; set; }
    public int UserID { get; set; }
    public Pool Pool { get; set; } = new();
}

public class Pool //TODO
{
    public bool Active { get; set; }
    public int ID { get; set; } = 1;
    public string Name { get; set; } = "osu!";
    public int RulesetID { get; set; }
    public int VariantID { get; set; }
}

public class MonthlyPlaycounts
{
    [JsonConverter(typeof(SimpleDateFormatConverter))]
    public DateTime StartDate { get; set; }
    public int Count { get; set; }
}

public class Page
{
    public string Html { get; set; } = string.Empty;
    public string Raw { get; set; } = string.Empty;
}

public class RankHighest
{
    public int Rank { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ReplaysWatchedCounts
{
    [JsonConverter(typeof(SimpleDateFormatConverter))]
    public DateTime StartDate { get; set; }
    public int Count { get; set; }
}

public class Statistics
{
    public int Count100 { get; set; }
    public int Count300 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }
    public Level Level { get; set; } = new();
    public int GlobalRank { get; set; }
    public double GlobalRankPercent { get; set; }
    public object? GlobalRankExp { get; set; } //TODO
    public double Pp { get; set; }
    public int PpExp { get; set; }
    public long RankedScore { get; set; }
    public double HitAccuracy { get; set; }
    public double Accuracy { get; set; }
    public int PlayCount { get; set; }
    public int PlayTime { get; set; }
    public long TotalScore { get; set; }
    public int TotalHits { get; set; }
    public int MaximumCombo { get; set; }
    public int ReplaysWatchedByOthers { get; set; }
    public bool IsRanked { get; set; }
    public GradeCounts GradeCounts { get; set; } = new();
    public int CountryRank { get; set; }
    public Rank Rank { get; set; } = new();
}

public class Level
{
    public int Current { get; set; }
    public int Progress { get; set; }
}

public class GradeCounts
{
    public int Ss { get; set; }
    public int Ssh { get; set; }
    public int S { get; set; }
    public int Sh { get; set; }
    public int A { get; set; }
}

public class Rank
{
    public int Country { get; set; }
}

public class StatisticsRulesets
{
    public Statistics Osu { get; set; } = new();
    public Statistics Taiko { get; set; } = new();
    public Statistics Fruits { get; set; } = new();
    public Statistics Mania { get; set; } = new();
}

public class Team
{
    public string FlagUrl { get; set; } = null!;
    public int ID { get; set; }
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
}

public class UserAchievements
{
    public DateTime AchievedAt { get; set; }
    public int AchievementID { get; set; }
}

public class RankHistory
{
    public string Mode { get; set; } = "osu"; //TODO same as playmode
    public int[] Data { get; set; } = [];
}