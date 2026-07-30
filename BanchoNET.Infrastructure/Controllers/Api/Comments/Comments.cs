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
        int page,
        string sort,
        int parentId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        if (!commentableType.Equals("beatmapset", StringComparison.OrdinalIgnoreCase))
            return BadRequest();
        
        //TODO reply comments have show more button (?)
        //TODO cant load nested comments by clicking show more

        return JsonSnake(
            await comments.GetBeatmapsetComments(commentableId, sort, uid, parentId == 0 ? null : parentId, page)
        );
    }

    [HttpPost]
    public async Task<ActionResult<CommentsResponse>> PostComment(
        [FromForm] CommentPostDto dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        if (!dto.CommentableType.Equals("beatmapset", StringComparison.OrdinalIgnoreCase))
            return BadRequest();
        
        return JsonSnake(await comments.PostComment(uid, dto.CommentableId, dto.Message, dto.ParentId));
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