using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Player;
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
    public int Ranked { get; set; }
    public DateTimeOffset? RankedDate { get; set; }
    public double Rating { get; set; }
    public bool Storyboard { get; set; }
    public DateTimeOffset SubmittedDate { get; set; }
    public string Tags { get; set; }
    public Availability Availability { get; set; }
    public bool HasFavourited { get; set; }
    public int[] Ratings { get; set; } = new int[10];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ApiBeatmap>? Beatmaps { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Nomination[]? CurrentNominations { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MapDescription? Description { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Genre? Genre { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Language? Language { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? PackTags { get; set; } = [];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<BasicApiPlayer>? RecentFavourites { get; set; } = [];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<BasicApiPlayer>? RelatedUsers { get; set; } = [];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SetTag>? RelatedTags { get; set; } = [];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BasicApiPlayer? User { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? VersionCount { get; set; }
    
    [JsonConstructor]
    public ApiBeatmapset() { }

    public ApiBeatmapset(
        BeatmapsetDto setDto,
        BeatmapDto? mapDto = null,
        bool assignBeatmapsList = true
    ) : base(setDto, mapDto) {
        var beatmaps = setDto.Beatmaps;
        var firstMap = mapDto ?? beatmaps.First();
        
        Bpm = firstMap.Bpm;
        CanBeHyped = false; //TODO
        DeletedAt = null; //TODO
        DiscussionEnabled = true; //TODO
        DiscussionLocked = false; //TODO
        IsScoreable = true; //TODO
        LastUpdated = firstMap.LastUpdate;
        LegacyThreadUrl = $"https://osu.{AppSettings.Domain}/community/forums/topics/0"; //TODO
        NominationsSummary = new NominationsSummary
        {
            Current = 2, //TODO
            EligibleMainRulesets = [ //TODO
                "osu"
            ],
            RequiredMeta = new RequiredMeta
            {
                MainRuleset = 2, //TODO
                NonMainRuleset = 1 //TODO
            }
        };
        Ranked = firstMap.Status == (sbyte)BeatmapStatus.Ranked ? 1 : 0;
        RankedDate = firstMap.RankedDate;
        Rating = 0; //TODO
        Storyboard = firstMap.HasStoryboard;
        SubmittedDate = firstMap.SubmitDate;
        Tags = firstMap.Tags;
        Availability = new Availability
        {
            DownloadDisabled = false, //TODO
            MoreInformation = null //TODO
        };
        HasFavourited = false; //TODO
        Ratings = [ //TODO
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0
        ];

        if (assignBeatmapsList)
        {
            List<ApiBeatmap> apiBeatmaps = [];
            apiBeatmaps.AddRange(
                from beatmap in beatmaps
                select new ApiBeatmap(beatmap, beatmap != null ? this : null)
            );

            Beatmaps = apiBeatmaps;
        }
        
        if (mapDto != null) return;
        
        CurrentNominations = [ //TODO
            new Nomination
            {
                BeatmapsetId = setDto.SetId,
                Rulesets = [ //TODO
                    "osu"
                ],
                Reset = false, //TODO
                UserId = 0 //TODO nominatorId
            },
            new Nomination
            {
                BeatmapsetId = setDto.SetId,
                Rulesets = [
                    "osu"
                ],
                Reset = false,
                UserId = 0
            }
        ];
        Description = new MapDescription
        {
            Description = "" //TODO
        };
        Genre = new Genre
        {
            Id = 0, //TODO
            Name = "Unknown" //TODO
        };
        Language = new Language
        {
            Id = 0, //TODO
            Name = "Unknown" //TODO
        };
        PackTags = [];
        RecentFavourites = []; //TODO
        RelatedUsers = [
            new BasicApiPlayer
            {
                //TODO fetch owner and others
            }
        ];
        RelatedTags = []; //TODO
        User = new BasicApiPlayer
        {
            //TODO fetch owner
        };
        VersionCount = 0; //TODO
    }
    
    public ApiBeatmapset(
        BeatmapSet beatmapset,
        Beatmap? beatmap = null,
        bool assignBeatmapsList = true
    ) : base(beatmapset, beatmap) {
        var firstMap = beatmap ?? beatmapset.Beatmaps[0];
        
        Bpm = firstMap.Bpm;
        CanBeHyped = false; //TODO
        DeletedAt = null; //TODO
        DiscussionEnabled = true; //TODO
        DiscussionLocked = false; //TODO
        IsScoreable = true; //TODO
        LastUpdated = firstMap.LastUpdate;
        LegacyThreadUrl = $"https://osu.{AppSettings.Domain}/community/forums/topics/0"; //TODO
        NominationsSummary = new NominationsSummary
        {
            Current = 2, //TODO
            EligibleMainRulesets = [ //TODO
                "osu"
            ],
            RequiredMeta = new RequiredMeta
            {
                MainRuleset = 2, //TODO
                NonMainRuleset = 1 //TODO
            }
        };
        Ranked = firstMap.Status == BeatmapStatus.Ranked ? 1 : 0;
        RankedDate = firstMap.RankedDate;
        Rating = 0; //TODO
        Storyboard = firstMap.HasStoryboard;
        SubmittedDate = firstMap.SubmitDate;
        Tags = firstMap.Tags;
        Availability = new Availability
        {
            DownloadDisabled = false, //TODO
            MoreInformation = null //TODO
        };
        HasFavourited = false; //TODO
        Ratings = [ //TODO
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0
        ];

        if (assignBeatmapsList)
        {
            List<ApiBeatmap> apiBeatmaps = [];
            apiBeatmaps.AddRange(
                from tempBeatmap in beatmapset.Beatmaps
                select new ApiBeatmap(tempBeatmap, beatmap != null ? this : null)
            );

            Beatmaps = apiBeatmaps;
        }

        if (beatmap != null) return;
        
        CurrentNominations = [ //TODO
            new Nomination
            {
                BeatmapsetId = firstMap.SetId,
                Rulesets = [ //TODO
                    "osu"
                ],
                Reset = false, //TODO
                UserId = 0 //TODO nominatorId
            },
            new Nomination
            {
                BeatmapsetId = firstMap.SetId,
                Rulesets = [
                    "osu"
                ],
                Reset = false,
                UserId = 0
            }
        ];
        Description = new MapDescription
        {
            Description = "" //TODO
        };
        Genre = new Genre
        {
            Id = 0, //TODO
            Name = "Unknown" //TODO
        };
        Language = new Language
        {
            Id = 0, //TODO
            Name = "Unknown" //TODO
        };
        PackTags = [];
        RecentFavourites = []; //TODO
        RelatedUsers = [
            new BasicApiPlayer
            {
                //TODO fetch owner and others
            }
        ];
        RelatedTags = []; //TODO
        User = new BasicApiPlayer
        {
            //TODO fetch owner
        };
        VersionCount = 0; //TODO
    }
}