using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Core.Models.Api.Comments;

public class CommentPostDto
{
    [FromForm(Name = "comment[commentable_type]")]
    public string CommentableType { get; set; } = null!;
    
    [FromForm(Name = "comment[commentable_id]")]
    public int CommentableId { get; set; }
    
    [FromForm(Name = "comment[message]")]
    public string Message { get; set; } = null!;
    
    [FromForm(Name = "comment[parent_id]")]
    public long? ParentId { get; set; }
}