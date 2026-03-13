namespace BanchoNET.Core.Models.Api.Beatmaps;

public class Nomination
{
    public int BeatmapsetId { get; set; }
    public List<string> Rulesets { get; set; } = [];
    public bool Reset { get; set; }
    public int UserId { get; set; }
}