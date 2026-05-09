using BanchoNET.Core.Models.Beatmaps;

namespace BanchoNET.Core.Models.Dtos;

public class BeatmapsetDto
{
    public int Id { get; set; }
    
    public string Artist { get; set; }
    public string ArtistUnicode { get; set; }
    
    public string Title { get; set; }
    public string TitleUnicode { get; set; }
    
    public bool IsRankedOfficially { get; set; }
    public bool IsPrivateUpload { get; set; }
    
    public BeatmapStatus Status { get; set; }
    public int FavoriteCount { get; set; }
    public int PlayCount { get; set; }
    public string Source { get; set; }
    public int GenreId { get; set; }
    public int LanguageId { get; set; }
    public bool Video { get; set; }
    public bool Storyboard { get; set; }
    public float Bpm { get; set; }
    public bool IsScoreable { get; set; }
    
    public string Tags { get; set; }
    public string Description { get; set; }
    
    public DateTimeOffset SubmittedDate { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset? RankedDate { get; set; }
    
    public int[] Ratings { get; set; } = new int[10];

    public string CreatorName { get; set; }
    public int CreatorId { get; set; }
    public PlayerDto Creator { get; set; } = null!;
    
    public int ThreadId { get; set; }
    public ThreadDto Thread { get; set; } = null!;
    
    public ICollection<BeatmapDto> Beatmaps { get; init; } = null!;
    public ICollection<BeatmapsetFavorite> BeatmapsetFavorites { get; set; } = [];
}