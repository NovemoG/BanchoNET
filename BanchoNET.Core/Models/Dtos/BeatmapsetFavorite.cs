namespace BanchoNET.Core.Models.Dtos;

public class BeatmapsetFavorite
{
    public int PlayerId { get; set; }
    public PlayerDto Player { get; set; } = null!;
    
    public int BeatmapsetId { get; set; }
    public BeatmapsetDto Beatmapset { get; set; } = null!;
    
    public DateTime FavoriteAt { get; set; }
}