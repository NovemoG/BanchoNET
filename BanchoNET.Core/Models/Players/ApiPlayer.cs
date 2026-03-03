namespace BanchoNET.Core.Models.Players;

public class ApiPlayer
{
    public string AvatarUrl { get; set; }
    public string CountryCode { get; set; }
    public string DefaultGroup { get; set; }
    public int ID { get; set; }
    public bool IsActive { get; set; }
    public bool IsBot { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsOnline { get; set; }
    public bool IsSupporter { get; set; }
    public object LastVisit { get; set; }
    public bool PmFriendsOnly { get; set; }
    public object ProfileColour { get; set; }
    public string Username { get; set; }
    public string CoverUrl { get; set; }
    public string Discord { get; set; }
    public bool HasSupported { get; set; }
    public string Interests { get; set; }
    public string JoinDate { get; set; }
    public string Location { get; set; }
    public int MaxBlocks { get; set; }
    public int MaxFriends { get; set; }
    public object Occupation { get; set; }
    public string Playmode { get; set; }
    public string[] Playstyle { get; set; }
    public int PostCount { get; set; }
    public int ProfileHue { get; set; }
    public string[] ProfileOrder { get; set; }
    public object Title { get; set; }
    public object TitleUrl { get; set; }
    public object Twitter { get; set; }
    public object Website { get; set; }
    public Country Country { get; set; }
    public Cover Cover { get; set; }
    public bool IsRestricted { get; set; }
    public Kudosu Kudosu { get; set; }
    public object[] AccountHistory { get; set; }
    public object ActiveTournamentBanner { get; set; }
    public object[] ActiveTournamentBanners { get; set; }
    public object[] Badges { get; set; }
    public int BeatmapPlaycountsCount { get; set; }
    public int CommentsCount { get; set; }
    public object CurrentSeasonStats { get; set; }
    public DailyChallengeUserStats DailyChallengeUserStats { get; set; }
    public int FavouriteBeatmapsetCount { get; set; }
    public int FollowerCount { get; set; }
    public int GraveyardBeatmapsetCount { get; set; }
    public object[] Groups { get; set; }
    public int GuestBeatmapsetCount { get; set; }
    public int LovedBeatmapsetCount { get; set; }
    public int MappingFollowerCount { get; set; }
    public MatchmakingStats[] MatchmakingStats { get; set; }
    public MonthlyPlaycounts[] MonthlyPlaycounts { get; set; }
    public int NominatedBeatmapsetCount { get; set; }
    public Page Page { get; set; }
    public int PendingBeatmapsetCount { get; set; }
    public string[] PreviousUsernames { get; set; }
    public RankHighest RankHighest { get; set; }
    public int RankedBeatmapsetCount { get; set; }
    public ReplaysWatchedCounts[] ReplaysWatchedCounts { get; set; }
    public int ScoresBestCount { get; set; }
    public int ScoresFirstCount { get; set; }
    public int ScoresPinnedCount { get; set; }
    public int ScoresRecentCount { get; set; }
    public object SessionVerificationMethod { get; set; }
    public bool SessionVerified { get; set; }
    public Statistics Statistics { get; set; }
    public StatisticsRulesets StatisticsRulesets { get; set; }
    public int SupportLevel { get; set; }
    public Team Team { get; set; }
    public UserAchievements[] UserAchievements { get; set; }
    public RankHistory? RankHistory { get; set; }
    public int RankedAndApprovedBeatmapsetCount { get; set; }
    public int UnrankedBeatmapsetCount { get; set; }
}

public class Country
{
    public string Code { get; set; }
    public string Name { get; set; }
}

public class Cover
{
    public string CustomUrl { get; set; }
    public string Url { get; set; }
    public object ID { get; set; }
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
    public string LastUpdate { get; set; }
    public string LastWeeklyStreak { get; set; }
    public int Playcount { get; set; }
    public int Top10PPlacements { get; set; }
    public int Top50PPlacements { get; set; }
    public int UserID { get; set; }
    public int WeeklyStreakBest { get; set; }
    public int WeeklyStreakCurrent { get; set; }
}

public class MatchmakingStats
{
    public int FirstPlacements { get; set; }
    public bool IsRatingProvisional { get; set; }
    public int Plays { get; set; }
    public int PoolID { get; set; }
    public int Rank { get; set; }
    public int Rating { get; set; }
    public int TotalPoints { get; set; }
    public int UserID { get; set; }
    public Pool Pool { get; set; }
}

public class Pool
{
    public bool Active { get; set; }
    public int ID { get; set; }
    public string Name { get; set; }
    public int RulesetID { get; set; }
    public int VariantID { get; set; }
}

public class MonthlyPlaycounts
{
    public string StartDate { get; set; }
    public int Count { get; set; }
}

public class Page
{
    public string Html { get; set; }
    public string Raw { get; set; }
}

public class RankHighest
{
    public int Rank { get; set; }
    public string UpdatedAt { get; set; }
}

public class ReplaysWatchedCounts
{
    public string StartDate { get; set; }
    public int Count { get; set; }
}

public class Statistics
{
    public int Count100 { get; set; }
    public int Count300 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }
    public Level Level { get; set; }
    public int GlobalRank { get; set; }
    public double GlobalRankPercent { get; set; }
    public object GlobalRankExp { get; set; }
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
    public GradeCounts GradeCounts { get; set; }
    public int CountryRank { get; set; }
    public Rank Rank { get; set; }
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
    public Osu Osu { get; set; }
    public Taiko Taiko { get; set; }
    public Fruits Fruits { get; set; }
    public Mania Mania { get; set; }
}

public class Osu
{
    public int Count100 { get; set; }
    public int Count300 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }
    public Level1 Level { get; set; }
    public int GlobalRank { get; set; }
    public double GlobalRankPercent { get; set; }
    public object GlobalRankExp { get; set; }
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
    public GradeCounts1 GradeCounts { get; set; }
}

public class Level1
{
    public int Current { get; set; }
    public int Progress { get; set; }
}

public class GradeCounts1
{
    public int Ss { get; set; }
    public int Ssh { get; set; }
    public int S { get; set; }
    public int Sh { get; set; }
    public int A { get; set; }
}

public class Taiko
{
    public int Count100 { get; set; }
    public int Count300 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }
    public Level2 Level { get; set; }
    public int GlobalRank { get; set; }
    public double GlobalRankPercent { get; set; }
    public object GlobalRankExp { get; set; }
    public double Pp { get; set; }
    public int PpExp { get; set; }
    public int RankedScore { get; set; }
    public double HitAccuracy { get; set; }
    public double Accuracy { get; set; }
    public int PlayCount { get; set; }
    public int PlayTime { get; set; }
    public int TotalScore { get; set; }
    public int TotalHits { get; set; }
    public int MaximumCombo { get; set; }
    public int ReplaysWatchedByOthers { get; set; }
    public bool IsRanked { get; set; }
    public GradeCounts2 GradeCounts { get; set; }
}

public class Level2
{
    public int Current { get; set; }
    public int Progress { get; set; }
}

public class GradeCounts2
{
    public int Ss { get; set; }
    public int Ssh { get; set; }
    public int S { get; set; }
    public int Sh { get; set; }
    public int A { get; set; }
}

public class Fruits
{
    public int Count100 { get; set; }
    public int Count300 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }
    public Level3 Level { get; set; }
    public object GlobalRank { get; set; }
    public object GlobalRankPercent { get; set; }
    public object GlobalRankExp { get; set; }
    public int Pp { get; set; }
    public int PpExp { get; set; }
    public int RankedScore { get; set; }
    public double HitAccuracy { get; set; }
    public double Accuracy { get; set; }
    public int PlayCount { get; set; }
    public int PlayTime { get; set; }
    public int TotalScore { get; set; }
    public int TotalHits { get; set; }
    public int MaximumCombo { get; set; }
    public int ReplaysWatchedByOthers { get; set; }
    public bool IsRanked { get; set; }
    public GradeCounts3 GradeCounts { get; set; }
}

public class Level3
{
    public int Current { get; set; }
    public int Progress { get; set; }
}

public class GradeCounts3
{
    public int Ss { get; set; }
    public int Ssh { get; set; }
    public int S { get; set; }
    public int Sh { get; set; }
    public int A { get; set; }
}

public class Mania
{
    public int Count100 { get; set; }
    public int Count300 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }
    public Level4 Level { get; set; }
    public int GlobalRank { get; set; }
    public double GlobalRankPercent { get; set; }
    public object GlobalRankExp { get; set; }
    public double Pp { get; set; }
    public int PpExp { get; set; }
    public int RankedScore { get; set; }
    public double HitAccuracy { get; set; }
    public double Accuracy { get; set; }
    public int PlayCount { get; set; }
    public int PlayTime { get; set; }
    public int TotalScore { get; set; }
    public int TotalHits { get; set; }
    public int MaximumCombo { get; set; }
    public int ReplaysWatchedByOthers { get; set; }
    public bool IsRanked { get; set; }
    public GradeCounts4 GradeCounts { get; set; }
}

public class Level4
{
    public int Current { get; set; }
    public int Progress { get; set; }
}

public class GradeCounts4
{
    public int Ss { get; set; }
    public int Ssh { get; set; }
    public int S { get; set; }
    public int Sh { get; set; }
    public int A { get; set; }
}

public class Team
{
    public string FlagUrl { get; set; }
    public int ID { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
}

public class UserAchievements
{
    public string AchievedAt { get; set; }
    public int AchievementID { get; set; }
}

public class RankHistory
{
    public string Mode { get; set; }
    public int[] Data { get; set; }
}