namespace BanchoNET.Core.Models.Api.Player;

public class ApiPlayer : BasicApiPlayer
{
    public string? CoverUrl { get; set; } //TODO this probably should have default value like AvatarUrl
    public string? Discord { get; set; }
    public bool HasSupported { get; set; }
    public string? Interests { get; set; }
    public DateTimeOffset JoinDate { get; set; }
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
    public bool IsRestricted { get; set; }
    public Kudosu Kudosu { get; set; } = new();
    public object[] AccountHistory { get; set; } = []; //TODO
    public object? ActiveTournamentBanner { get; set; } //TODO
    public object[] ActiveTournamentBanners { get; set; } = []; //TODO
    public Badge[] Badges { get; set; } = [];
    public int BeatmapPlaycountsCount { get; set; }
    public int CommentsCount { get; set; }
    public object? CurrentSeasonStats { get; set; } //TODO
    public DailyChallengeUserStats DailyChallengeUserStats { get; set; } = new();
    public int FavouriteBeatmapsetCount { get; set; }
    public int FollowerCount { get; set; }
    public int GraveyardBeatmapsetCount { get; set; }
    public Group[] Groups { get; set; } = [];
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
    public Statistics Statistics { get; set; } = new(); //TODO statistics for mode set in playmode
    public int SupportLevel { get; set; }
    public UserAchievements[] UserAchievements { get; set; } = [];
    public RankHistory RankHistory { get; set; } = new();
    public int RankedAndApprovedBeatmapsetCount { get; set; }
    public int UnrankedBeatmapsetCount { get; set; }
}