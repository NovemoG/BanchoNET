namespace BanchoNET.Core.Models.Dtos;

public class SkillsDto
{
    public long Id { get; set; }
    
    public Dictionary<SkillType, float> Skills { get; set; } = new();
    
    public PlayerDto? Player { get; set; }
    public int? PlayerId { get; set; }
    
    public BeatmapDto? Beatmap { get; set; }
    public int? BeatmapId { get; set; }
}