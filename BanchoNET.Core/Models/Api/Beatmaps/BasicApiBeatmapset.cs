using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class BasicApiBeatmapset
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

    [JsonConstructor]
    public BasicApiBeatmapset() { }

    public BasicApiBeatmapset(
        BeatmapsetDto beatmapset,
        BeatmapDto? beatmap = null
    ) {
        var beatmaps = beatmapset.Beatmaps;
        var firstMap = beatmap ?? beatmaps.First();
        
        AnimeCover = false; //TODO
        Artist = firstMap.Artist;
        ArtistUnicode = firstMap.ArtistUnicode;
        Covers = new Covers(beatmapset.SetId, firstMap.CoverId);
        Creator = firstMap.CreatorName;
        FavouriteCount = 0; //TODO
        GenreId = 0; //TODO
        Hype = null; //TODO
        Id = beatmapset.SetId;
        LanguageId = 0; //TODO
        Nsfw = false; //TODO
        Offset = 0; //TODO
        PlayCount = beatmaps.Sum(b => b.Plays);
        PreviewUrl = $"//b.{AppSettings.Domain}/preview/{beatmapset.SetId}.mp3";
        Source = ""; //TODO
        Spotlight = false; //TODO
        Status = ((BeatmapStatus)firstMap.Status).ToApiBeatmapStatus();
        Title = firstMap.Title;
        TitleUnicode = firstMap.TitleUnicode;
        TrackId = null; //TODO
        UserId = firstMap.CreatorId;
        Video = firstMap.HasVideo;
    }

    public BasicApiBeatmapset(
        BeatmapSet beatmapset,
        Beatmap? beatmap = null
    ) {
        var firstMap = beatmap ?? beatmapset.Beatmaps[0];
        
        AnimeCover = false; //TODO
        Artist = firstMap.Artist;
        ArtistUnicode = firstMap.ArtistUnicode;
        Covers = new Covers(firstMap.SetId, firstMap.CoverId);
        Creator = firstMap.Creator;
        FavouriteCount = 0; //TODO
        GenreId = 0; //TODO
        Hype = null; //TODO
        Id = firstMap.SetId;
        LanguageId = 0; //TODO
        Nsfw = false; //TODO
        Offset = 0; //TODO
        PlayCount = beatmapset.Beatmaps.Sum(b => b.Plays);
        PreviewUrl = $"//b.{AppSettings.Domain}/preview/{firstMap.SetId}.mp3";
        Source = ""; //TODO
        Spotlight = false; //TODO
        Status = firstMap.Status.ToApiBeatmapStatus();
        Title = firstMap.Title;
        TitleUnicode = firstMap.TitleUnicode;
        TrackId = null; //TODO
        UserId = firstMap.CreatorId;
        Video = firstMap.HasVideo;
    }
}