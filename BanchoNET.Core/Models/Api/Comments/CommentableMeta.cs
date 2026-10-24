namespace BanchoNET.Core.Models.Api.Comments;

public class CommentableMeta : CommentMeta
{
    public int Id { get; set; }
    public required string Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public int? OwnerId { get; set; }
    public string? OwnerTitle { get; set; }
    public bool Locked { get; set; }
    
    public CommentableUserAttributes CurrentUserAttributes { get; set; } = new();
}

public class CommentableUserAttributes
{
    public string? CanNewCommentReason { get; set; }
}

public readonly record struct Commentable(string Type, int Id)
{
    public const string BEATMAPSET = "beatmapset";
    public const string BUILD = "build";

    public static Commentable? Parse(
        string? type,
        int id
    ) => type?.ToLowerInvariant() switch
    {
        BEATMAPSET => new Commentable(BEATMAPSET, id),
        BUILD => new Commentable(BUILD, id),
        _ => null
    };
}