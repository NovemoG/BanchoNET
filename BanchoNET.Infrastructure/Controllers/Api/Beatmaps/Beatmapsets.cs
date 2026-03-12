using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;
using ApiBeatmap = BanchoNET.Core.Models.Api.Beatmaps.ApiBeatmap;

// ReSharper disable once CheckNamespace
namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("beatmapsets/{beatmapsetId:int}")]
    public async Task<ActionResult<ApiBeatmapset>> GetBeatmapset(
        int beatmapsetId
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
        var beatmapset = await beatmaps.GetBeatmapSet(beatmapsetId);
        if (beatmapset == null) return NotFound(); //TODO
        
        var firstMap = beatmapset.Beatmaps[0];

        var apiBeatmapset = new ApiBeatmapset
        {
            AnimeCover = false, //TODO
            Artist = firstMap.Artist,
            ArtistUnicode = firstMap.ArtistUnicode,
            Covers = new Covers(beatmapsetId, firstMap.CoverId),
            Creator = firstMap.Creator,
            FavouriteCount = 0, //TODO
            GenreId = 0, //TODO
            Hype = null, //TODO
            Id = beatmapset.Id,
            LanguageId = 0, //TODO
            Nsfw = false, //TODO
            Offset = 0, //TODO
            PlayCount = beatmapset.Beatmaps.Sum(b => b.Plays),
            PreviewUrl = $"//b.ppy.sh/preview/{beatmapsetId}.mp3",
            Source = "", //TODO
            Spotlight = false, //TODO
            Status = firstMap.Status.ToApiBeatmapStatus(),
            Title = firstMap.Title,
            TitleUnicode = firstMap.TitleUnicode,
            TrackId = null, //TODO
            UserId = firstMap.CreatorId,
            Video = firstMap.HasVideo,
            Bpm = firstMap.Bpm,
            CanBeHyped = false, //TODO
            DeletedAt = null, //TODO
            DiscussionEnabled = true, //TODO
            DiscussionLocked = false, //TODO
            IsScoreable = true, //TODO
            LastUpdated = firstMap.LastUpdate,
            LegacyThreadUrl = "https://osu.ppy.sh/community/forums/topics/0", //TODO
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
            },
            Ranked = firstMap.Status == BeatmapStatus.Ranked ? 1 : 0,
            RankedDate = firstMap.RankedDate,
            Rating = 0, //TODO
            Storyboard = firstMap.HasStoryboard,
            SubmittedDate = firstMap.SubmitDate,
            Tags = firstMap.Tags,
            Availability = new Availability
            {
                DownloadDisabled = false, //TODO
                MoreInformation = null //TODO
            },
            HasFavourited = false, //TODO
            CurrentNominations = [ //TODO
                new Nomination
                {
                    BeatmapsetId = beatmapsetId,
                    Rulesets = [ //TODO
                        "osu"
                    ],
                    Reset = false, //TODO
                    UserId = 0 //TODO nominatorId
                },
                new Nomination
                {
                    BeatmapsetId = beatmapsetId,
                    Rulesets = [
                        "osu"
                    ],
                    Reset = false,
                    UserId = 0
                }
            ],
            Description = new MapDescription
            {
                Description = "" //TODO
            },
            Genre = new Genre
            {
                Id = 0, //TODO
                Name = "Unknown" //TODO
            },
            Language = new Language
            {
                Id = 0, //TODO
                Name = "Unknown" //TODO
            },
            PackTags = [],
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
            ],
            RecentFavourites = [], //TODO
            RelatedUsers = [
                new BasicApiPlayer
                {
                    //TODO fetch owner and others
                }
            ],
            RelatedTags = [], //TODO
            User = new BasicApiPlayer
            {
                //TODO fetch owner
            },
            VersionCount = 0 //TODO
        };

        List<ApiBeatmap> apiBeatmaps = [];
        apiBeatmaps.AddRange(
            from beatmap in beatmapset.Beatmaps
            select new ApiBeatmap
            {
                BeatmapsetId = beatmap.SetId,
                DifficultyRating = beatmap.StarRating,
                Id = beatmap.Id,
                Mode = EnumExtensions.FromModeMap[beatmap.Mode],
                Status = beatmap.Status.ToApiBeatmapStatus(),
                TotalLength = beatmap.TotalLength,
                UserId = beatmap.CreatorId,
                Version = beatmap.Name,
                Accuracy = beatmap.Od,
                Ar = beatmap.Ar,
                Bpm = beatmap.Bpm,
                Convert = false, //TODO
                CountCircles = beatmap.NotesCount,
                CountSliders = beatmap.SlidersCount,
                CountSpinners = beatmap.SpinnersCount,
                Cs = beatmap.Cs,
                DeletedAt = null, //TODO
                Drain = beatmap.Hp,
                HitLength = beatmap.HitLength,
                IsScoreable = true, //TODO
                LastUpdated = beatmap.LastUpdate,
                ModeInt = (int)beatmap.Mode,
                Passcount = beatmap.Passes,
                Playcount = beatmap.Plays,
                Ranked = beatmap.Status == BeatmapStatus.Ranked ? 1 : 0,
                Url = $"https://osu.ppy.sh/beatmaps/{beatmap.Id}",
                Checksum = beatmap.MD5,
                CurrentUserPlaycount = 0, //TODO fetch
                CurrentUserTagIds = [], //TODO
                Failtimes = new Failtime
                {
                    Fail = [], //TODO
                    Exit = [] //TODO
                },
                MaxCombo = beatmap.MaxCombo,
                Owners = [], //TODO
                TopTagIds = [] //TODO
            }
        );

        apiBeatmapset.Beatmaps = apiBeatmaps;
        
        return new JsonResult(apiBeatmapset, SnakeCaseNamingPolicy.Options);
    }
}