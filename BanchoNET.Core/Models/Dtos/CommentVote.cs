namespace BanchoNET.Core.Models.Dtos;

public class CommentVote
{
    public int PlayerId { get; set; }
    public PlayerDto Player { get; set; } = null!;
    
    public long CommentId { get; set; }
    public CommentDto Comment { get; set; } = null!;
}