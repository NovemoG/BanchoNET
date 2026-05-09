using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Api.Comments;

public class Comment
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public int UserId { get; set; }
    public bool Pinned { get; set; }
    public int RepliesCount { get; set; }
    public int VotesCount { get; set; }
    public string CommentableType { get; set; } = "beatmapset";
    public int CommentableId { get; set; }
    public string? LegacyName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public int? EditedById { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageHtml { get; set; }
    
    [JsonConstructor]
    public Comment() { }

    public Comment(
        CommentDto comment,
        bool pinned = false
    ) {
        Id = comment.Id;
        ParentId = comment.ReplyToId;
        UserId = comment.AuthorId;
        Pinned = pinned;
        RepliesCount = comment.RepliesCount;
        VotesCount = comment.VotesCount;
        CommentableId = comment.ThreadId;
        CreatedAt = comment.CreatedAt;
        UpdatedAt = comment.UpdatedAt;
        DeletedAt = comment.DeletedAt;
        EditedAt = comment.EditedAt;
        EditedById = comment.EditedById;
        Message = comment.Message;
        MessageHtml = comment.MessageHtml;
    }
}