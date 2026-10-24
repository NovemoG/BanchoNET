using System.Net;
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
	private const int PageSize = 50;

	public async Task<CommentsResponse?> GetComments(
		Commentable commentable,
		string sort,
		int userId,
		long? parentId = null,
		int page = 1
	) {
		var meta = await GetMeta(commentable);
		if (meta == null) return null;

		var thread = await FindThread(commentable).AsNoTracking().FirstOrDefaultAsync();
		if (thread == null)
			return new CommentsResponse { Sort = sort, CommentableMeta = [meta] };

		var baseQuery = dbContext.Comments
			.AsNoTracking()
			.Where(c => c.ThreadId == thread.Id);

		var totalComments = await baseQuery.CountAsync();
		var topLevelCount = await baseQuery.CountAsync(c => c.ReplyToId == null);

		// The client requests the replies of a comment with its ID as parent_id, and top-level comments without one.
		var levelQuery = baseQuery
			.Include(c => c.Author)
			.Where(c => c.ReplyToId == parentId);

		levelQuery = sort switch
		{
			"old" => levelQuery.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
			"top" => levelQuery.OrderByDescending(c => c.VotesCount).ThenByDescending(c => c.Id),
			_ => levelQuery.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id),
		};

		var comments = await levelQuery
			.Skip((Math.Max(page, 1) - 1) * PageSize)
			.Take(PageSize + 1)
			.ToListAsync();

		var hasMore = comments.Count > PageSize;
		if (hasMore) comments.RemoveAt(PageSize);

		var pinnedComments = parentId == null && page <= 1
			? await baseQuery
				.Include(c => c.Author)
				.Where(c => c.Pinned && c.ReplyToId == null)
				.ToListAsync()
			: [];

		var parentIds = comments.Concat(pinnedComments).Select(c => c.Id).Distinct().ToList();

		// Direct replies are included, so a comment's replies show without another request.
		// Deeper replies are loaded when the client asks for them by parent ID.
		var includedComments = parentIds.Count == 0 ? [] : await baseQuery
			.Include(c => c.Author)
			.Where(c => c.ReplyToId != null && parentIds.Contains(c.ReplyToId.Value))
			.ToListAsync();

		var allVisibleComments = comments.Concat(pinnedComments).Concat(includedComments).ToList();
		var repliesCounts = await GetRepliesCounts(baseQuery, allVisibleComments);

		return new CommentsResponse
		{
			Comments = comments.Select(ToApi).ToList(),
			PinnedComments = pinnedComments.Select(ToApi).ToList(),
			IncludedComments = includedComments.Select(ToApi).ToList(),
			Users = UniqueUsers(allVisibleComments),
			UserVotes = await GetUserVotes(allVisibleComments, userId),
			UserFollow = await dbContext.ThreadFollows.AnyAsync(f => f.ThreadId == thread.Id && f.PlayerId == userId),
			HasMore = hasMore,
			HasMoreId = hasMore ? comments[^1].Id : 0,
			Sort = sort,
			TopLevelCount = topLevelCount,
			Total = totalComments,
			CommentableMeta = MetaList(meta, allVisibleComments)
		};
		
		Comment ToApi(CommentDto c) => new(c, commentable, repliesCounts.GetValueOrDefault(c.Id));
	}

	public async Task<CommentsResponse?> PostComment(
		int userId,
		Commentable commentable,
		string message,
		long? replyToId = null
	) {
		if (await GetMeta(commentable) == null) return null;

		var thread = await FindThread(commentable).FirstOrDefaultAsync();
		if (thread == null)
		{
			thread = commentable.Type == Commentable.BUILD
				? new ThreadDto { BuildId = commentable.Id }
				: new ThreadDto { BeatmapsetId = commentable.Id };
			dbContext.Threads.Add(thread);

			await dbContext.SaveChangesAsync();
		}

		if (replyToId.HasValue && !await dbContext.Comments.AnyAsync(c => c.Id == replyToId.Value && c.ThreadId == thread.Id))
			return null;

		var newComment = new CommentDto
		{
			ThreadId = thread.Id,
			AuthorId = userId,
			ReplyToId = replyToId,
			Message = message,

			// osu! API uses markdown.
			MessageHtml = $"<div class='osu-md osu-md--comment'><p>{WebUtility.HtmlEncode(message)}</p></div>",

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

		return await GetTargetedComment(commentId, userId) ?? new CommentsResponse();
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

		return await GetTargetedComment(commentId, userId) ?? new CommentsResponse();
	}

	/// <summary>
	/// The single comment the client just posted or voted on.
	/// </summary>
	private async Task<CommentsResponse?> GetTargetedComment(
		long targetCommentId,
		int currentUserId
	) {
		var targetComment = await dbContext.Comments
			.Include(c => c.Author)
			.Include(c => c.Thread)
			.AsNoTracking()
			.FirstAsync(c => c.Id == targetCommentId);

		var commentable = targetComment.Thread.BuildId is { } buildId
			? new Commentable(Commentable.BUILD, buildId)
			: new Commentable(Commentable.BEATMAPSET, targetComment.Thread.BeatmapsetId!.Value);

		var meta = await GetMeta(commentable);
		if (meta == null) return null;

		var baseQuery = dbContext.Comments
			.AsNoTracking()
			.Where(c => c.ThreadId == targetComment.ThreadId);

		var repliesCounts = await GetRepliesCounts(baseQuery, [targetComment]);

		return new CommentsResponse
		{
			Comments = [new Comment(targetComment, commentable, repliesCounts.GetValueOrDefault(targetComment.Id))],
			Users = UniqueUsers([targetComment]),
			UserVotes = await GetUserVotes([targetComment], currentUserId),
			UserFollow = await dbContext.ThreadFollows
				.AnyAsync(f => f.ThreadId == targetComment.ThreadId && f.PlayerId == currentUserId),
			Cursor = new Cursor { Id = targetComment.Id, CreatedAt = targetComment.CreatedAt },
			Total = await baseQuery.CountAsync(),
			TopLevelCount = await baseQuery.CountAsync(c => c.ReplyToId == null),
			CommentableMeta = MetaList(meta, [targetComment])
		};
	}

	private IQueryable<ThreadDto> FindThread(
		Commentable commentable
	) => commentable.Type == Commentable.BUILD
		? dbContext.Threads.Where(t => t.BuildId == commentable.Id)
		: dbContext.Threads.Where(t => t.BeatmapsetId == commentable.Id);
	
	private async Task<CommentableMeta?> GetMeta(
		Commentable commentable
	) {
		if (commentable.Type == Commentable.BUILD)
		{
			var build = LazerChangelog.FindBuild(await PublishedLazerReleases.List(), commentable.Id);
			if (build == null) return null;

			var parts = build.Version.Split('-');

			return new CommentableMeta
			{
				Id = commentable.Id,
				Type = Commentable.BUILD,
				Title = LazerChangelog.DisplayName(build),
				Url = $"https://osu.{AppSettings.Domain}/home/changelog/{parts[1]}/{parts[0]}"
			};
		}

		var beatmapset = await dbContext.Beatmapsets
			.AsNoTracking()
			.FirstOrDefaultAsync(bs => bs.Id == commentable.Id);

		return beatmapset == null ? null : new CommentableMeta
		{
			Id = beatmapset.Id,
			Type = Commentable.BEATMAPSET,
			Title = beatmapset.Title,
			Url = $"https://osu.{AppSettings.Domain}/beatmapsets/{beatmapset.Id}",
			OwnerId = beatmapset.CreatorId,
			OwnerTitle = "MAPPER"
		};
	}
	
	private static async Task<Dictionary<long, int>> GetRepliesCounts(
		IQueryable<CommentDto> threadComments,
		List<CommentDto> comments
	) {
		var ids = comments.Select(c => c.Id).Distinct().ToList();
		if (ids.Count == 0) return [];

		return await threadComments
			.Where(c => c.ReplyToId != null && ids.Contains(c.ReplyToId.Value))
			.GroupBy(c => c.ReplyToId!.Value)
			.Select(g => new { g.Key, Count = g.Count() })
			.ToDictionaryAsync(g => g.Key, g => g.Count);
	}

	private async Task<List<long>> GetUserVotes(
		List<CommentDto> comments,
		int userId
	) {
		var ids = comments.Select(c => c.Id).Distinct().ToList();
		if (ids.Count == 0) return [];

		return await dbContext.CommentVotes
			.Where(v => v.PlayerId == userId && ids.Contains(v.CommentId))
			.Select(v => v.CommentId)
			.ToListAsync();
	}

	private static List<BasicApiPlayer> UniqueUsers(
		List<CommentDto> comments
	) => comments
		.Select(c => c.Author)
		.DistinctBy(a => a.Id)
		.Select(u => new BasicApiPlayer(u))
		.ToList();

	private static List<CommentMeta> MetaList(
		CommentableMeta meta,
		List<CommentDto> visibleComments
	) {
		List<CommentMeta> list = [meta];

		if (visibleComments.Any(c => c.DeletedAt != null))
			list.Add(new CommentMeta { Title = "Deleted Item" });

		return list;
	}
}