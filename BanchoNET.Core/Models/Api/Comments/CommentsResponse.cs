using BanchoNET.Core.Models.Api.Player;

namespace BanchoNET.Core.Models.Api.Comments;

public class CommentsResponse
{
    public List<Comment> Comments { get; set; } = [];
    public bool HasMore { get; set; }
    public long HasMoreId { get; set; }
    public List<Comment> IncludedComments { get; set; } = [];
    public List<Comment> PinnedComments { get; set; } = [];
    public List<long> UserVotes { get; set; } = [];
    public bool UserFollow { get; set; }
    public List<BasicApiPlayer> Users { get; set; } = [];
    public string Sort { get; set; } = "new";
    public Cursor Cursor { get; set; } = new();
    public int TopLevelCount { get; set; }
    public int Total { get; set; }
    public List<CommentMeta> CommentableMeta { get; set; } = [];
}