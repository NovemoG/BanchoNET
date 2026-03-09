namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ApiBeatmap
{
    public int BeatmapsetId { get; set; }
    public double DifficultyRating { get; set; }
    public int Id { get; set; }
    public string Mode { get; set; }
    public string Status { get; set; }
    public int TotalLength { get; set; }
    public int UserId { get; set; }
    public string Version { get; set; }
    public int Accuracy { get; set; }
    public int Ar { get; set; }
    public double Bpm { get; set; }
    public bool Convert { get; set; }
    public int CountCircles { get; set; }
    public int CountSliders { get; set; }
    public int CountSpinners { get; set; }
    public int Cs { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public int Drain { get; set; }
    public int HitLength { get; set; }
    public bool IsScoreable { get; set; }
    public string LastUpdated { get; set; }
    public int ModeInt { get; set; }
    public int Passcount { get; set; }
    public int Playcount { get; set; }
    public int Ranked { get; set; }
    public string Url { get; set; }
    public string Checksum { get; set; }
    public ApiBeatmapset Beatmapset { get; set; }
    public int CurrentUserPlaycount { get; set; }
    public Failtimes Failtimes { get; set; }
    public int MaxCombo { get; set; }
    public Owners[]? Owners { get; set; }
}

public class ApiBeatmapset
{
    public bool AnimeCover { get; set; }
    public string Artist { get; set; }
    public string ArtistUnicode { get; set; }
    public Covers Covers { get; set; }
    public string Creator { get; set; }
    public int FavouriteCount { get; set; }
    public int GenreId { get; set; }
    public int? Hype { get; set; }
    public int Id { get; set; }
    public int LanguageId { get; set; }
    public bool Nsfw { get; set; }
    public int Offset { get; set; }
    public int PlayCount { get; set; }
    public string PreviewUrl { get; set; }
    public string Source { get; set; }
    public bool Spotlight { get; set; }
    public string Status { get; set; }
    public string Title { get; set; }
    public string TitleUnicode { get; set; }
    public object TrackId { get; set; } //TODO
    public int UserId { get; set; }
    public bool Video { get; set; }
    public double Bpm { get; set; }
    public bool CanBeHyped { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool DiscussionEnabled { get; set; }
    public bool DiscussionLocked { get; set; }
    public bool IsScoreable { get; set; }
    public string LastUpdated { get; set; }
    public string LegacyThreadUrl { get; set; }
    public NominationsSummary NominationsSummary { get; set; }
    public int Ranked { get; set; }
    public string RankedDate { get; set; }
    public double Rating { get; set; }
    public bool Storyboard { get; set; }
    public string SubmittedDate { get; set; }
    public string Tags { get; set; }
    public Availability Availability { get; set; }
    public bool HasFavourited { get; set; }
    public int[] Ratings { get; set; } = new int[10];
}

public class Covers
{
    public string Cover { get; set; }
    public string Cover2X { get; set; }
    public string Card { get; set; }
    public string Card2X { get; set; }
    public string List { get; set; }
    public string List2X { get; set; }
    public string Slimcover { get; set; }
    public string Slimcover2X { get; set; }
}

public class NominationsSummary
{
    public int Current { get; set; }
    public string[] EligibleMainRulesets { get; set; }
    public RequiredMeta RequiredMeta { get; set; }
}

public class RequiredMeta
{
    public int MainRuleset { get; set; }
    public int NonMainRuleset { get; set; }
}

public class Availability
{
    public bool DownloadDisabled { get; set; }
    public string? MoreInformation { get; set; }
}

public class Failtimes
{
    public int[] Fail { get; set; } = [];
    public int[] Exit { get; set; } = [];
}

public class Owners
{
    public int Id { get; set; }
    public string Username { get; set; }
}

