using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Models.Dtos;

[PrimaryKey(nameof(Id))]
[Index(nameof(BeatmapsetId), IsUnique = true)]
public class ThreadDto
{
    public int Id { get; set; }
    
    [ForeignKey(nameof(BeatmapsetId))]
    public BeatmapsetDto Beatmapset { get; set; } = null!;
    public int BeatmapsetId { get; set; }

    public ICollection<CommentDto> Comments { get; set; } = [];
    public ICollection<ThreadFollows> Follows { get; set; } = [];
}