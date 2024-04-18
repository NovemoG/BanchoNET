using Newtonsoft.Json;

namespace BanchoNET.Models;

public class DirectBeatmapSet
{
    public int SetId { get; set; }
    public int RankedStatus { get; set; }
    public DateTime SubmittedDate { get; set; }
    public DateTime ApprovedDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public DateTime LastChecked { get; set; }
    public string Artist { get; set; }
    public string Title { get; set; }
    public string Creator { get; set; }
    public int CreatorId { get; set; }
    public string Source { get; set; }
    public string Tags { get; set; }
    public string HasVideo { get; set; }
    public int Favourites { get; set; }
    [JsonProperty("ChildrenBeatmaps")]
    public List<DirectBeatmap>? Beatmaps { get; set; }
}

public class DirectBeatmap
{
    public int ParentSetId { get; set; }
    public int BeatmapId { get; set; }
    public int TotalLength { get; set; }
    public int HitLength { get; set; }
    public string DiffName { get; set; }
    public string FileMD5 { get; set; }
    public double Cs { get; set; }
    public double Ar { get; set; }
    public double Hp { get; set; }
    public double Od { get; set; }
    public int Mode { get; set; }
    public double Bpm { get; set; }
    public int Playcount { get; set; }
    public int Passcount { get; set; }
    public int MaxCombo { get; set; }
    public double DifficultyRating { get; set; }
}