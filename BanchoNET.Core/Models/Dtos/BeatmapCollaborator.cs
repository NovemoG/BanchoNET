using System.ComponentModel.DataAnnotations;

namespace BanchoNET.Core.Models.Dtos;

public class BeatmapCollaborator
{
    public int BeatmapId { get; set; }
    public BeatmapDto Beatmap { get; set; } = null!;
    
    public int OwnerId { get; set; }

    [MaxLength(32)]
    public string OwnerName { get; set; } = "";
}