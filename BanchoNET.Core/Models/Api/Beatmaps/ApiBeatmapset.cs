using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ApiBeatmapset : BasicApiBeatmapset
{
    public double Bpm { get; set; }
    public bool CanBeHyped { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool DiscussionEnabled { get; set; }
    public bool DiscussionLocked { get; set; }
    public bool IsScoreable { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public string LegacyThreadUrl { get; set; }
    public NominationsSummary NominationsSummary { get; set; }
    public int Ranked { get; set; } //TODO status but int
    public DateTimeOffset? RankedDate { get; set; }
    public double Rating { get; set; }
    public bool Storyboard { get; set; }
    public DateTimeOffset SubmittedDate { get; set; }
    public string Tags { get; set; }
    public Availability Availability { get; set; }
    public bool HasFavourited { get; set; }
    public int[] Ratings { get; set; } = new int[10];
    
    [JsonConstructor]
    public ApiBeatmapset() { }

    public ApiBeatmapset(
        BeatmapsetDto beatmapset
    ) : base(beatmapset) {
        Bpm = beatmapset.Bpm;
        CanBeHyped = false;
        DeletedAt = null;
        DiscussionEnabled = true;
        DiscussionLocked = false;
        IsScoreable = beatmapset.IsScoreable;
        LastUpdated = beatmapset.LastUpdated;
        LegacyThreadUrl = $"https://osu.{AppSettings.Domain}/community/forums/topics/0"; //TODO
        NominationsSummary = new NominationsSummary
        {
            Current = 1,
            EligibleMainRulesets = [
                EnumExtensions.FromModeMap[beatmapset.Beatmaps.First().Mode]
            ],
            RequiredMeta = new RequiredMeta
            {
                MainRuleset = 1, 
                NonMainRuleset = 1
            }
        };
        Ranked = (int)beatmapset.Status;
        RankedDate = beatmapset.RankedDate;
        Rating = beatmapset.Ratings.Average();
        Storyboard = beatmapset.Storyboard;
        SubmittedDate = beatmapset.SubmittedDate;
        Tags = beatmapset.Tags;
        Availability = new Availability
        {
            DownloadDisabled = false,
            MoreInformation = null
        };
        HasFavourited = false; //TODO
        Ratings = beatmapset.Ratings;
    }
    
    public ApiBeatmapset(
        Beatmapset beatmapset
    ) : base(beatmapset) {
        Bpm = beatmapset.Bpm;
        CanBeHyped = false;
        DeletedAt = null;
        DiscussionEnabled = true;
        DiscussionLocked = false;
        IsScoreable = beatmapset.IsScoreable;
        LastUpdated = beatmapset.LastUpdate;
        LegacyThreadUrl = $"https://osu.{AppSettings.Domain}/community/forums/topics/0"; //TODO
        NominationsSummary = new NominationsSummary
        {
            Current = 1,
            EligibleMainRulesets = [
                EnumExtensions.FromModeMap[beatmapset.Beatmaps.First().Mode]
            ],
            RequiredMeta = new RequiredMeta
            {
                MainRuleset = 1, 
                NonMainRuleset = 1
            }
        };
        Ranked = (int)beatmapset.Status;
        RankedDate = beatmapset.RankedDate;
        Rating = beatmapset.Rating;
        Storyboard = beatmapset.Storyboard;
        SubmittedDate = beatmapset.SubmitDate;
        Tags = beatmapset.Tags;
        Availability = new Availability
        {
            DownloadDisabled = false,
            MoreInformation = null
        };
        HasFavourited = false; //TODO
        Ratings = beatmapset.Ratings;
    }
}