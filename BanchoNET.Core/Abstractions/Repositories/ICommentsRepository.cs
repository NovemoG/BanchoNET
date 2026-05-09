using BanchoNET.Core.Models.Api.Comments;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface ICommentsRepository
{
    Task<CommentsResponse> GetBeatmapsetComments(
        int beatmapsetId,
        string sort,
        int userId,
        long? parentId = null,
        int page = 1
    );

    Task<CommentsResponse> PostComment(
        int userId,
        int beatmapsetId,
        string message,
        long? replyToId = null
    );

    Task<CommentsResponse> AddVote(
        long commentId,
        int userId
    );

    Task<CommentsResponse> RemoveVote(
        long commentId,
        int userId
    );
}