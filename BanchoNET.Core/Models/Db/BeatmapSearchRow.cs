using BanchoNET.Core.Models.Beatmaps;
using NpgsqlTypes;

namespace BanchoNET.Core.Models.Db;

public class BeatmapSearchRow
{
    public int Id { get; set; }
    public int SetId { get; set; }

    public GameMode Mode { get; set; }
    public BeatmapStatus Status { get; set; }
    public int GenreId { get; set; }
    public int LanguageId { get; set; }
    public bool Nsfw { get; set; }

    public float StarRating { get; set; }
    public float Bpm { get; set; }
    public float Cs { get; set; }
    public float Ar { get; set; }
    public float Od { get; set; }
    public float Hp { get; set; }
    public int CirclesCount { get; set; }
    public int SlidersCount { get; set; }
    public int SpinnersCount { get; set; }
    public int MaxCombo { get; set; }
    public int TotalLength { get; set; }
    public int HitLength { get; set; }
    public long Plays { get; set; }
    public int Favorites { get; set; }
    public float Rating { get; set; }

    public string Version { get; set; } = "";
    public string Artist { get; set; } = "";
    public string ArtistUnicode { get; set; } = "";
    public string Title { get; set; } = "";
    public string TitleUnicode { get; set; } = "";
    public string Source { get; set; } = "";
    public string Tags { get; set; } = "";
    public string Description { get; set; } = "";
    public string CreatorName { get; set; } = "";

    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset SubmittedDate { get; set; }
    public DateTimeOffset? RankedDate { get; set; }

    public NpgsqlTsVector SearchVector { get; set; } = null!;
}