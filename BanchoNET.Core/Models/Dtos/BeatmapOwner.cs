using System.ComponentModel.DataAnnotations;

namespace BanchoNET.Core.Models.Dtos;

public class BeatmapOwner
{
    public int PlayerId { get; set; }
    public PlayerDto Player { get; set; } = null!;
    
    public int BeatmapId { get; set; }
    public BeatmapDto Beatmap { get; set; } = null!;
    
    [MaxLength(16)]
    public string Username { get; set; } = null!;
}