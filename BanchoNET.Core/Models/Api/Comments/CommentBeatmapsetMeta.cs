namespace BanchoNET.Core.Models.Api.Comments;

public class CommentBeatmapsetMeta : CommentMeta
{
    public int Id { get; set; }
    public string Type { get; set; } = "beatmapset";
    public string Url { get; set; } = null!;
    public int OwnerId { get; set; }
    public string OwnerTitle { get; set; } = "MAPPER";
    public bool Locked { get; set; }
}