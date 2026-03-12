using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Models.Dtos;

[PrimaryKey(nameof(SetId))]
public class BeatmapsetDto
{
    [Key] public int SetId { get; set; }
    public int OwnerId { get; set; }
    
    [ForeignKey(nameof(OwnerId))]
    public PlayerDto Owner { get; set; } = null!;
    
    public ICollection<BeatmapDto> Beatmaps { get; init; } = null!;
}