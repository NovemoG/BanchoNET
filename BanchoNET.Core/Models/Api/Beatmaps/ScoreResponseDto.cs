namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ScoreResponseDto
{
    public int BeatmapId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long Id { get; set; }
    public int? PlaylistItemId { get; set; } //TODO
    public int UserId { get; set; }
}