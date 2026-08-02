using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Beatmaps;

/// <summary>
/// This should always be retrieved from the official API or database
/// as it contains data that is not necessary to be stored in memory
/// </summary>
public class ApiBeatmapsetFull : ApiBeatmapset
{
    public List<ApiBeatmap> Beatmaps { get; set; }
    public Nomination[] CurrentNominations { get; set; }
    public MapDescription Description { get; set; }
    public Genre Genre { get; set; }
    public Language Language { get; set; }
    public List<string> PackTags { get; set; }
    public List<BasicApiPlayer> RecentFavourites { get; set; }
    public List<BasicApiPlayer> RelatedUsers { get; set; } = [];
    public List<SetTag> RelatedTags { get; set; }
    public BasicApiPlayer User { get; set; }
    public int VersionCount { get; set; }
    
    [JsonConstructor]
    public ApiBeatmapsetFull() { }

    public ApiBeatmapsetFull(
        BeatmapsetDto beatmapset
    ) : base(beatmapset) {
        Beatmaps = beatmapset.Beatmaps.Select(b => new ApiBeatmap(b)).ToList();
        CurrentNominations = !beatmapset.Status.HasNominations() ? [] : [
            new Nomination
            {
                BeatmapsetId = beatmapset.Id,
                Rulesets = [
                    EnumExtensions.FromModeMap[beatmapset.Beatmaps.First().Mode]
                ],
                Reset = false,
                UserId = 1
            },
        ];
        Description = new MapDescription
        {
            Description = beatmapset.Description
        };
        Genre = new Genre
        {
            Id = beatmapset.GenreId,
            Name = "Unknown" //TODO
        };
        Language = new Language
        {
            Id = beatmapset.LanguageId,
            Name = "Unknown" //TODO
        };
        PackTags = [];
        RecentFavourites = beatmapset.BeatmapsetFavorites.Select(bf => new BasicApiPlayer(bf.Player)).ToList();
        RelatedTags = [];
        VersionCount = 0;
        
        User = new BasicApiPlayer(beatmapset.Creator);
        RelatedUsers.Add(User);
    }

    public ApiBeatmapsetFull(
        Beatmapset beatmapset
    ) : base(beatmapset) {
        Beatmaps = beatmapset.Beatmaps.Select(b => new ApiBeatmap(b)).ToList();
        CurrentNominations = [];
        Description = new MapDescription
        {
            Description = beatmapset.Description
        };
        Genre = new Genre
        {
            Id = beatmapset.GenreId,
            Name = "Unknown" //TODO
        };
        Language = new Language
        {
            Id = beatmapset.LanguageId,
            Name = "Unknown" //TODO
        };
        PackTags = [];
        RecentFavourites = [];
        RelatedTags = [];
        RelatedUsers = [];
        User = new BasicApiPlayer();
        VersionCount = 0;
    }
}