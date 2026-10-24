namespace BanchoNET.Core.Models.Dtos;

public class BeatmapsetChange
{
    public int Id { get; set; }
    public int BeatmapsetId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}