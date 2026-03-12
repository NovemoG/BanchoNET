using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Player;

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
    public float Accuracy { get; set; }
    public float Ar { get; set; }
    public double Bpm { get; set; }
    public bool Convert { get; set; }
    public int CountCircles { get; set; }
    public int CountSliders { get; set; }
    public int CountSpinners { get; set; }
    public float Cs { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public float Drain { get; set; }
    public int HitLength { get; set; }
    public bool IsScoreable { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public int ModeInt { get; set; }
    public long Passcount { get; set; }
    public long Playcount { get; set; }
    public int Ranked { get; set; }
    public string Url { get; set; }
    public string Checksum { get; set; }
    [JsonIgnore]
    public ApiBeatmapset Beatmapset { get; set; }
    public int CurrentUserPlaycount { get; set; }
    public string[] CurrentUserTagIds { get; set; } = [];
    public Failtime Failtimes { get; set; }
    public int MaxCombo { get; set; }
    public List<Owner>? Owners { get; set; }
    public List<MapTag> TopTagIds { get; set; } = [];
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
    public long PlayCount { get; set; }
    public string PreviewUrl { get; set; }
    public string Source { get; set; }
    public bool Spotlight { get; set; }
    public string Status { get; set; }
    public string Title { get; set; }
    public string TitleUnicode { get; set; }
    public object? TrackId { get; set; } //TODO
    public int UserId { get; set; }
    public bool Video { get; set; }
    public double Bpm { get; set; }
    public bool CanBeHyped { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool DiscussionEnabled { get; set; }
    public bool DiscussionLocked { get; set; }
    public bool IsScoreable { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public string LegacyThreadUrl { get; set; }
    public NominationsSummary NominationsSummary { get; set; }
    public int Ranked { get; set; }
    public DateTimeOffset? RankedDate { get; set; }
    public double Rating { get; set; }
    public bool Storyboard { get; set; }
    public DateTimeOffset SubmittedDate { get; set; }
    public string Tags { get; set; }
    public Availability Availability { get; set; }
    public bool HasFavourited { get; set; }
    public List<ApiBeatmap> Beatmaps { get; set; }
    public Nomination[] CurrentNominations { get; set; }
    public MapDescription Description { get; set; }
    public Genre Genre { get; set; }
    public Language Language { get; set; }
    public List<string> PackTags { get; set; } = [];
    public int[] Ratings { get; set; } = new int[10];
    public List<BasicApiPlayer> RecentFavourites { get; set; } = [];
    public List<BasicApiPlayer> RelatedUsers { get; set; } = [];
    public List<SetTag> RelatedTags { get; set; } = [];
    public BasicApiPlayer User { get; set; }
    public int VersionCount { get; set; }
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

    public Covers(
        int beatmapsetId,
        long coverId
    ) {
        Cover = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/cover.jpg?{coverId}";
        Cover2X = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/cover@2x.jpg?{coverId}";
        Card = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/card.jpg?{coverId}";
        Card2X = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/card@2x.jpg?{coverId}";
        List = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/list.jpg?{coverId}";
        List2X = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/list@2x.jpg?{coverId}";
        Slimcover = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/slimcover.jpg?{coverId}";
        Slimcover2X = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/slimcover@2x.jpg?{coverId}";
    }
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

public class Failtime
{
    public int[] Fail { get; set; } = [];
    public int[] Exit { get; set; } = [];
}

public class Owner
{
    public int Id { get; set; }
    public string Username { get; set; }
}

public class Nomination
{
    public int BeatmapsetId { get; set; }
    public List<string> Rulesets { get; set; } = [];
    public bool Reset { get; set; }
    public int UserId { get; set; }
}

public class MapDescription
{
    public string Description { get; set; }
}

public class Genre
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Language
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class MapTag
{
    public int TagId { get; set; }
    public int Count { get; set; }
}

public class SetTag
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int RulesetId { get; set; }
    public string Description { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}