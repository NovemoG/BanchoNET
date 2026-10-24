using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api.Comments;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Comments;

[Route("api/v2/comments")]
public partial class CommentsController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    ICommentsRepository comments
) : ApiControllerBase(players, playerService, beatmaps)
{
    [HttpGet]
    public async Task<ActionResult<CommentsResponse>> GetComments(
        [FromQuery(Name = "commentable_id")] int commentableId,
        [FromQuery(Name = "commentable_type")] string commentableType,
        int page = 1,
        string sort = "new",
        [FromQuery(Name = "parent_id")] long? parentId = null
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        if (Commentable.Parse(commentableType, commentableId) is not { } commentable)
            return BadRequest();
        
        // A parent ID of 0 means top-level comments.
        var response = await comments.GetComments(commentable, sort, uid, parentId is null or 0 ? null : parentId, page);

        return response == null ? NotFound() : JsonSnake(response);
    }

    [HttpPost]
    public async Task<ActionResult<CommentsResponse>> PostComment(
        [FromForm] CommentPostDto dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        if (Commentable.Parse(dto.CommentableType, dto.CommentableId) is not { } commentable
            || string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest();
        }
        
        var response = await comments.PostComment(uid, commentable, dto.Message, dto.ParentId);
        
        return response == null ? NotFound() : JsonSnake(response);
    }
    
    [HttpPost("{commentId:long}/vote")]
    public async Task<ActionResult<CommentsResponse>> PostCommentVote(
        long commentId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        return JsonSnake(await comments.AddVote(commentId, uid));
    }
    
    [HttpDelete("{commentId:long}/vote")]
    public async Task<ActionResult<CommentsResponse>> DeleteCommentVote(
        long commentId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        return JsonSnake(await comments.RemoveVote(commentId, uid));
    }
}