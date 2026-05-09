using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Api.Comments;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class CommentsRepository(BanchoDbContext dbContext) : ICommentsRepository
{
	public async Task<CommentsResponse> GetBeatmapsetComments(
		int beatmapsetId,
		string sort,
		int userId,
		long? parentId = null,
		int page = 1
	) {
		var take = (page - 1) * 50 + 50;
		
		var thread = await dbContext.Threads
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.BeatmapsetId == beatmapsetId);
		
		var beatmapset = await dbContext.Beatmapsets
			.AsNoTracking()
			.Where(bs => bs.Id == beatmapsetId)
			.FirstOrDefaultAsync();

		if (thread == null && beatmapset != null)
		{
			thread = new ThreadDto { BeatmapsetId = beatmapsetId };
			dbContext.Threads.Add(thread);
			
			await dbContext.SaveChangesAsync();
		}
		else if (thread == null || beatmapset == null)
		{
			return new CommentsResponse();
		}
		
		var baseQuery = dbContext.Comments
			.AsNoTracking()
			.Where(c => c.ThreadId == thread.Id);
		
		var totalComments = await baseQuery.CountAsync();
		var topLevelCount = await baseQuery.CountAsync(c => c.ReplyToId == null);
		
		var pinnedComments = await baseQuery
			.Include(c => c.Author)
			.Where(c => c.Pinned)
			.ToListAsync();

		var commentsQuery = baseQuery
			.Include(c => c.Author)
			.Where(c => c.ReplyToId == null)
			.AsQueryable();
		
		commentsQuery = sort switch
		{
			"old" => commentsQuery.OrderBy(c => c.CreatedAt),
			"new" => commentsQuery.OrderByDescending(c => c.CreatedAt),
			"top" => commentsQuery.OrderByDescending(c => c.VotesCount),
			_ => commentsQuery.OrderByDescending(c => c.CreatedAt),
		};
		
		if (parentId.HasValue)
			commentsQuery = commentsQuery.Where(c => c.Id < parentId.Value);
		
		var mainComments = await commentsQuery.Take(take + 1).ToListAsync();
		
		var hasMore = mainComments.Count > take;
		if (hasMore) mainComments.RemoveAt(take);
		
		var nextId = mainComments.LastOrDefault()?.Id ?? 0;

		var visibleTopLevelIds = mainComments
			.Select(c => c.Id)
			.Concat(pinnedComments.Select(c => c.Id))
			.Distinct()
			.ToList();
		
		List<CommentDto> includedComments = [];
		if (visibleTopLevelIds.Count > 0)
		{
			includedComments = await baseQuery
				.Include(c => c.Author)
				.Where(c => c.ReplyToId != null && visibleTopLevelIds.Contains(c.ReplyToId.Value))
				.ToListAsync();
		}
		
		var allVisibleComments = mainComments
			.Concat(pinnedComments)
			.Concat(includedComments)
			.ToList();
		
		var allVisibleIds = allVisibleComments
			.Select(c => c.Id)
			.Distinct()
			.ToList();

		List<long> userVotes = [];
		if (allVisibleIds.Count > 0)
		{
			userVotes = await dbContext.CommentVotes
				.Where(v => v.PlayerId == userId && allVisibleIds.Contains(v.CommentId))
				.Select(v => v.CommentId)
				.ToListAsync();
		}
		
		var userFollows = await dbContext.ThreadFollows
			.AnyAsync(f => f.ThreadId == thread.Id && f.PlayerId == userId);
		
		var uniqueUsers = allVisibleComments
			.Select(c => c.Author)
			.GroupBy(a => a.Id)
			.Select(g => g.First())
			.Select(u => new BasicApiPlayer(u))
			.ToList();

		var metaList = GenerateMetaList(allVisibleComments, beatmapset);

		return new CommentsResponse
		{
			Comments = mainComments.Select(c => new Comment(c)).ToList(),
			PinnedComments = pinnedComments.Select(c => new Comment(c)).ToList(),
			IncludedComments = includedComments.Select(c => new Comment(c)).ToList(),
			Users = uniqueUsers,
			UserVotes = userVotes,
			UserFollow = userFollows,
			HasMore = hasMore,
			HasMoreId = hasMore ? nextId : 0,
			Sort = sort,
			TopLevelCount = topLevelCount,
			Total = totalComments,
			CommentableMeta = metaList
		};
	}

	public async Task<CommentsResponse> PostComment(
		int userId,
		int beatmapsetId,
		string message,
		long? replyToId = null
	) {
		var thread = await dbContext.Threads
			.FirstOrDefaultAsync(t => t.BeatmapsetId == beatmapsetId);
		
		if (thread == null)
		{
			thread = new ThreadDto { BeatmapsetId = beatmapsetId };
			dbContext.Threads.Add(thread);
			
			await dbContext.SaveChangesAsync();
		}
		
		if (replyToId.HasValue)
		{
			var parentExists = await dbContext.Comments
				.AnyAsync(c => c.Id == replyToId.Value && c.ThreadId == thread.Id);
            
			if (!parentExists) 
				throw new Exception("Parent comment not found or belongs to a different thread.");
		}
		
		var newComment = new CommentDto
		{
			ThreadId = thread.Id,
			AuthorId = userId,
			ReplyToId = replyToId,
			Message = message,
        
			// Note: osu! API uses markdown. You'd typically run your markdown parser here.
			// e.g., MessageHtml = MarkdownHelper.ToHtml(message), 
			MessageHtml = $"<div class='osu-md osu-md--comment'><p>{message}</p></div>", 
        
			CreatedAt = DateTimeOffset.UtcNow,
			VotesCount = 0,
			Pinned = false
		};

		dbContext.Comments.Add(newComment);
		await dbContext.SaveChangesAsync();
		
		return await GetTargetedComment(newComment.Id, userId);
	}

	public async Task<CommentsResponse> AddVote(
		long commentId,
		int userId
	) {
		var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
		if (comment == null) return new CommentsResponse();
		
		var existingVote = await dbContext.CommentVotes
			.AnyAsync(v => v.CommentId == commentId && v.PlayerId == userId);

		if (!existingVote)
		{
			dbContext.CommentVotes.Add(new CommentVote 
			{ 
				CommentId = commentId, 
				PlayerId = userId 
			});
            
			comment.VotesCount++;
			await dbContext.SaveChangesAsync();
		}

		return await GetTargetedComment(commentId, userId);
	}
	
	public async Task<CommentsResponse> RemoveVote(
		long commentId,
		int userId
	) {
		var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
		if (comment == null) return new CommentsResponse();

		var vote = await dbContext.CommentVotes
			.FirstOrDefaultAsync(v => v.CommentId == commentId && v.PlayerId == userId);

		if (vote != null)
		{
			dbContext.CommentVotes.Remove(vote);
			comment.VotesCount--;
			await dbContext.SaveChangesAsync();
		}

		return await GetTargetedComment(commentId, userId);
	}

	private async Task<CommentsResponse> GetTargetedComment(
		long targetCommentId,
		int currentUserId
	) {
		var targetComment = await dbContext.Comments
		    .Include(c => c.Author)
		    .Include(c => c.Thread)
				.ThenInclude(t => t.Beatmapset)
		    .AsNoTracking()
		    .FirstAsync(c => c.Id == targetCommentId);

		var baseQuery = dbContext.Comments
		    .AsNoTracking()
		    .Where(c => c.ThreadId == targetComment.ThreadId);
	    
	    var totalComments = await baseQuery.CountAsync();
	    var topLevelCount = await baseQuery.CountAsync(c => c.ReplyToId == null);
	    
	    var pinnedComments = await baseQuery
	        .Include(c => c.Author)
	        .Where(c => c.Pinned)
	        .ToListAsync();
	    
	    var targetTopLevelIds = pinnedComments.Where(c => c.ReplyToId == null).Select(c => c.Id).ToList();
	    
	    if (targetComment.ReplyToId == null)
	        targetTopLevelIds.Add(targetComment.Id);
	    
	    var distinctTopLevelIds = targetTopLevelIds.Distinct().ToList();

	    var includedComments = new List<CommentDto>();
	    if (distinctTopLevelIds.Count > 0)
	    {
	        includedComments = await baseQuery
	            .Include(c => c.Author)
	            .Where(c => c.ReplyToId != null && distinctTopLevelIds.Contains(c.ReplyToId.Value))
	            .ToListAsync();
	    }
	    
	    var allVisibleComments = new List<CommentDto> { targetComment };
	    allVisibleComments.AddRange(pinnedComments);
	    allVisibleComments.AddRange(includedComments);
	    var visibleCommentIds = allVisibleComments.Select(c => c.Id).Distinct().ToList();
	    
	    var userVotes = await dbContext.CommentVotes
	        .Where(v => v.PlayerId == currentUserId && visibleCommentIds.Contains(v.CommentId))
	        .Select(v => v.CommentId)
	        .ToListAsync();

	    var userFollows = await dbContext.ThreadFollows
	        .AnyAsync(f => f.ThreadId == targetComment.ThreadId && f.PlayerId == currentUserId);

	    var uniqueUsers = allVisibleComments
		    .Select(c => c.Author)
	        .GroupBy(a => a.Id)
		    .Select(g => g.First())
		    .Select(u => new BasicApiPlayer(u))
		    .ToList();
	    
	    var metaList = GenerateMetaList(allVisibleComments, targetComment.Thread.Beatmapset);

	    return new CommentsResponse
	    {
		    Comments = [new Comment(targetComment)],
		    PinnedComments = pinnedComments.Select(c => new Comment(c)).ToList(),
		    IncludedComments = [],
		    Users = uniqueUsers,
		    UserVotes = userVotes,
		    UserFollow = userFollows,
		    HasMore = false,
		    HasMoreId = 0,
		    Cursor = new Cursor { Id = targetComment.Id, CreatedAt = targetComment.CreatedAt },
		    Total = totalComments,
		    TopLevelCount = topLevelCount,
		    CommentableMeta = metaList
	    };
	}

	private List<CommentMeta> GenerateMetaList(
		List<CommentDto> visibleComments,
		BeatmapsetDto beatmapset
	) {
		List<CommentMeta> meta = [
			new CommentBeatmapsetMeta
			{
				Id = beatmapset.Id,
				Title = beatmapset.Title,
				Url = $"https://osu.{AppSettings.Domain}/beatmapsets/{beatmapset.Id}",
				OwnerId = beatmapset.CreatorId,
				Locked = false
			}
		];

		if (visibleComments.Any(c => c.DeletedAt != null))
			meta.Add(new CommentMeta { Title = "Deleted Item" });

		return meta;
	}
}