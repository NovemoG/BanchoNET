namespace BanchoNET.Core.Models.Dtos;

public class BeatmapPlays
{
    public int PlayerId { get; set; }
    public PlayerDto Player { get; set; } = null!;
    
    public int BeatmapId { get; set; }
    public BeatmapDto Beatmap { get; set; } = null!;
    
    public int Plays { get; set; }
}