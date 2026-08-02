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
    public Hype? Hype { get; set; }
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
    public object? TrackId { get; set; }
    public int UserId { get; set; }
    public bool Video { get; set; }

    [JsonConstructor]
    public BasicApiBeatmapset() { }

    public BasicApiBeatmapset(
        BeatmapsetDto beatmapset
    ) {
        AnimeCover = false;
        Artist = beatmapset.Artist;
        ArtistUnicode = beatmapset.ArtistUnicode;
        Covers = new Covers(beatmapset.Id);
        Creator = beatmapset.CreatorName;
        FavouriteCount = beatmapset.FavoriteCount;
        GenreId = beatmapset.GenreId;
        Hype = null;
        Id = beatmapset.Id;
        LanguageId = beatmapset.LanguageId;
        Nsfw = beatmapset.Nsfw;
        Offset = 0; //TODO
        PlayCount = beatmapset.PlayCount;
        PreviewUrl = $"//b.{AppSettings.Domain}/preview/{beatmapset.Id}.mp3";
        Source = beatmapset.Source;
        Spotlight = false;
        Status = beatmapset.Status.ToApiBeatmapStatus();
        Title = beatmapset.Title;
        TitleUnicode = beatmapset.TitleUnicode;
        TrackId = null;
        UserId = beatmapset.CreatorId;
        Video = beatmapset.Video;
    }

    public BasicApiBeatmapset(
        Beatmapset beatmapset
    ) {
        AnimeCover = false;
        Artist = beatmapset.Artist;
        ArtistUnicode = beatmapset.ArtistUnicode;
        Covers = new Covers(beatmapset.Id);
        Creator = beatmapset.CreatorName;
        FavouriteCount = beatmapset.FavoriteCount;
        GenreId = beatmapset.GenreId;
        Hype = null;
        Id = beatmapset.Id;
        LanguageId = beatmapset.LanguageId;
        Nsfw = beatmapset.Nsfw;
        Offset = 0; //TODO
        PlayCount = beatmapset.PlayCount;
        PreviewUrl = $"//b.{AppSettings.Domain}/preview/{beatmapset.Id}.mp3";
        Source = beatmapset.Source;
        Spotlight = false;
        Status = beatmapset.Status.ToApiBeatmapStatus();
        Title = beatmapset.Title;
        TitleUnicode = beatmapset.TitleUnicode;
        TrackId = null;
        UserId = beatmapset.CreatorId;
        Video = beatmapset.Video;
    }
}