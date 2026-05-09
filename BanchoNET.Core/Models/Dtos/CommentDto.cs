namespace BanchoNET.Core.Models.Dtos;

public class CommentDto
{
    public long Id { get; set; }

    public string? Message { get; set; }
    public string? MessageHtml { get; set; }
    
    public int VotesCount { get; set; }
    public int RepliesCount { get; set; }
    public bool Pinned { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public int? EditedById { get; set; }
    
    public PlayerDto? Editor { get; set; }
    public PlayerDto Author { get; set; } = null!;
    public int AuthorId { get; set; }

    public ThreadDto Thread { get; set; } = null!;
    public int ThreadId { get; set; }
    
    public CommentDto? ReplyTo { get; set; }
    public long? ReplyToId { get; set; }
    
    public ICollection<CommentDto> Replies { get; set; } = [];
}