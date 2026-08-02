using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Db.Configurations;
using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BanchoNET.Core.Models.Db;

public sealed class BanchoDbContext : DbContext
{
	private const string SearchVectorSql =
		"""
		setweight(to_tsvector('simple', coalesce("Title", '') || ' ' || coalesce("TitleUnicode", '')), 'A') ||
		setweight(to_tsvector('simple', coalesce("Artist", '') || ' ' || coalesce("ArtistUnicode", '')), 'B') ||
		setweight(to_tsvector('simple', coalesce("CreatorName", '')), 'C') ||
		setweight(to_tsvector('simple', coalesce("OwnerNames", '') || ' ' || coalesce("Version", '') || ' ' || coalesce("Source", '') || ' ' || coalesce("Tags", '')), 'D')
		""";
	
	private readonly ISearchProjectionSyncService _projectionSync = null!;

	public BanchoDbContext(
		DbContextOptions<BanchoDbContext> options,
		ISearchProjectionSyncService projectionSync
	) : base(options) {
		_projectionSync = projectionSync;
	}

	internal BanchoDbContext(
		DbContextOptions<BanchoDbContext> options
	) : base(options) {
	}
	
	public DbSet<PlayerDto> Players { get; init; } = null!;
	public DbSet<StatsDto> Stats { get; init; } = null!;
	public DbSet<RelationshipDto> Relationships { get; init; } = null!;
	public DbSet<LoginDto> PlayerLogins { get; init; } = null!;
	public DbSet<ClientHashesDto> ClientHashes { get; init; } = null!;
	
	public DbSet<ScoreDto> Scores { get; init; } = null!;
	public DbSet<ReplayWatches> ReplayWatches { get; init; } = null!;
	
	public DbSet<BeatmapDto> Beatmaps { get; init; } = null!;
	public DbSet<BeatmapsetDto> Beatmapsets { get; init; } = null!;
	public DbSet<BeatmapCollaborator> BeatmapCollaborators { get; init; } = null!;
	public DbSet<BeatmapsetFavorite> BeatmapsetFavorites { get; init; } = null!;
	public DbSet<BeatmapPlays> BeatmapPlays { get; init; } = null!;
	
	public DbSet<MessageDto> Messages { get; init; } = null!;
	public DbSet<ChannelDto> Channels { get; init; } = null!;
	public DbSet<ChannelPlayer> ChannelPlayers { get; init; } = null!;
	
	public DbSet<ThreadDto> Threads { get; init; } = null!;
	public DbSet<CommentDto> Comments { get; init; } = null!;
	public DbSet<ThreadFollows> ThreadFollows { get; init; } = null!;
	public DbSet<CommentVote> CommentVotes { get; init; } = null!;

	public DbSet<BeatmapSearchRow> BeatmapSearch { get; init; } = null!;

	public DbSet<PlayerHistoryDto> PlayerHistories { get; init; } = null!;

	public DbSet<MultiplayerMatchDto> MultiplayerMatches { get; init; } = null!;
	public DbSet<MultiplayerGameDto> MultiplayerGames { get; init; } = null!;
	public DbSet<MultiplayerScoreDto> MultiplayerScores { get; init; } = null!;
	public DbSet<MultiplayerEventDto> MultiplayerEvents { get; init; } = null!;
	public DbSet<MultiplayerParticipantDto> MultiplayerParticipants { get; init; } = null!;

	public DbSet<ReleaseDto> Releases { get; init; } = null!;
	public DbSet<RefreshToken> RefreshTokens { get; init; } = null!;
	public DbSet<SessionVerification> SessionVerifications { get; init; } = null!;
	public DbSet<OAuthClient> OAuthClients { get; init; } = null!;

	protected override void OnModelCreating(
		ModelBuilder modelBuilder
	) {
		var multiplayerHistory = new MultiplayerHistoryConfiguration();

		modelBuilder
			.ApplyConfiguration(new PlayerConfiguration())
			.ApplyConfiguration(new RelationshipConfiguration())
			.ApplyConfiguration(new BeatmapConfiguration())
			.ApplyConfiguration(new BeatmapsetConfiguration())
			.ApplyConfiguration(new MessageConfiguration())
			.ApplyConfiguration(new ScoreConfiguration())
			.ApplyConfiguration(new SkillsConfiguration())
			.ApplyConfiguration(new CommentConfiguration())
			.ApplyConfiguration(new PlayerHistoryConfiguration())
			.ApplyConfiguration<MultiplayerMatchDto>(multiplayerHistory)
			.ApplyConfiguration<MultiplayerGameDto>(multiplayerHistory)
			.ApplyConfiguration<MultiplayerScoreDto>(multiplayerHistory)
			.ApplyConfiguration<MultiplayerEventDto>(multiplayerHistory)
			.ApplyConfiguration<MultiplayerParticipantDto>(multiplayerHistory);

		modelBuilder.Entity<RefreshToken>(entity =>
		{
			entity.HasIndex(x => x.TokenHash).IsUnique();
			entity.HasIndex(x => x.UserId);
			entity.HasIndex(x => x.FamilyId);
			entity.HasIndex(x => x.ExpiresAt);
		});

		modelBuilder.Entity<OAuthClient>(entity =>
		{
			// Id is the client_id, so it must never be generated for us
			entity.Property(x => x.Id).ValueGeneratedNever();
		});

		modelBuilder.Entity<ChannelPlayer>(entity =>
		{
			entity.HasKey(x => new { x.PlayerId, x.ChannelId });

			entity.HasIndex(x => x.LastReadMessageId);

			entity.HasOne(x => x.Player)
				.WithMany(p => p.PlayerChannels)
				.HasForeignKey(x => x.PlayerId)
				.OnDelete(DeleteBehavior.Cascade);
			
			entity.HasOne(x => x.Channel)
				.WithMany(c => c.ChannelPlayers)
				.HasForeignKey(x => x.ChannelId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		modelBuilder.Entity<BeatmapCollaborator>(entity =>
		{
			entity.HasKey(x => new { x.BeatmapId, x.OwnerId });

			entity.HasIndex(x => x.OwnerId);

			entity.Property(x => x.OwnerName)
				.HasMaxLength(32)
				.IsUnicode(false);
			
			entity.HasOne(x => x.Beatmap)
				.WithMany(b => b.Collaborators)
				.HasForeignKey(x => x.BeatmapId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		modelBuilder.Entity<BeatmapPlays>(entity =>
		{
			entity.HasKey(x => new { x.PlayerId, x.BeatmapId });

			entity.HasIndex(x => x.Plays);

			entity.HasOne(x => x.Player)
				.WithMany(p => p.PlayedBeatmaps)
				.HasForeignKey(x => x.PlayerId)
				.OnDelete(DeleteBehavior.Cascade);
			
			entity.HasOne(x => x.Beatmap)
				.WithMany(c => c.PlaysData)
				.HasForeignKey(x => x.BeatmapId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		modelBuilder.Entity<BeatmapsetFavorite>(entity =>
		{
			entity.HasKey(x => new { x.PlayerId, x.BeatmapsetId });

			entity.HasIndex(x => x.FavoriteAt);
			
			entity.Property(x => x.FavoriteAt)
				.HasDefaultValueSql("CURRENT_TIMESTAMP");

			entity.HasOne(x => x.Player)
				.WithMany(p => p.FavoriteBeatmapsets)
				.HasForeignKey(x => x.PlayerId)
				.OnDelete(DeleteBehavior.Cascade);
			
			entity.HasOne(x => x.Beatmapset)
				.WithMany(c => c.BeatmapsetFavorites)
				.HasForeignKey(x => x.BeatmapsetId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		modelBuilder.Entity<ReplayWatches>(entity =>
		{
			entity.HasKey(x => new { x.PlayerId, x.ScoreId });

			entity.HasIndex(x => x.Count);

			entity.HasOne(x => x.Player)
				.WithMany(p => p.WatchedReplays)
				.HasForeignKey(x => x.PlayerId)
				.OnDelete(DeleteBehavior.Cascade);
			
			entity.HasOne(x => x.Score)
				.WithMany(c => c.ReplayWatches)
				.HasForeignKey(x => x.ScoreId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		modelBuilder.Entity<CommentVote>(entity =>
		{
			entity.HasKey(x => new { x.PlayerId, x.CommentId });

			entity.HasOne(x => x.Player)
				.WithMany(p => p.CommentVotes)
				.HasForeignKey(x => x.PlayerId)
				.OnDelete(DeleteBehavior.Cascade);
			
			entity.HasOne(x => x.Comment)
				.WithMany()
				.HasForeignKey(x => x.CommentId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		modelBuilder.Entity<ThreadFollows>(entity =>
		{
			entity.HasKey(x => new { x.PlayerId, x.ThreadId });

			entity.HasOne(x => x.Player)
				.WithMany(p => p.ThreadFollows)
				.HasForeignKey(x => x.PlayerId)
				.OnDelete(DeleteBehavior.Cascade);
			
			entity.HasOne(x => x.Thread)
				.WithMany(x => x.Follows)
				.HasForeignKey(x => x.ThreadId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		modelBuilder.Entity<BeatmapSearchRow>(b =>
		{
			b.ToTable("BeatmapSearch");
			b.HasKey(x => x.Id);

			b.Property(x => x.SearchVector)
				.HasComputedColumnSql(SearchVectorSql, stored: true);

			b.HasIndex(x => x.SearchVector)
				.HasMethod("GIN");
			
			b.HasIndex(x => new {
					x.Title,
					x.TitleUnicode,
					x.Artist,
					x.ArtistUnicode,
					x.Version,
					x.Source,
					x.Tags,
					x.CreatorName,
					x.OwnerNames
				})
				.HasMethod("GIN")
				.HasOperators(
					"gin_trgm_ops", // for Title
					"gin_trgm_ops", // for TitleUnicode
					"gin_trgm_ops", // for Artist
					"gin_trgm_ops", // for ArtistUnicode
					"gin_trgm_ops", // for Version
					"gin_trgm_ops", // for Source
					"gin_trgm_ops", // for Tags
					"gin_trgm_ops", // for CreatorName
					"gin_trgm_ops"  // for OwnerNames
				);
			
			b.HasIndex(x => new { x.Mode, x.Status, x.GenreId, x.LanguageId, x.Nsfw });
			b.HasIndex(x => x.CreatorId);
			b.HasIndex(x => x.OwnerIds).HasMethod("GIN");
			b.HasIndex(x => x.SetId);
			b.HasIndex(x => new { x.Bpm, x.Cs, x.Ar, x.Od, x.Hp, x.StarRating, x.Plays, x.Favorites, x.Rating });
		});
		
		modelBuilder.HasPostgresExtension("pg_trgm");
		
		base.OnModelCreating(modelBuilder);
	}

	public override async Task<int> SaveChangesAsync(
		CancellationToken cancellationToken = default
	) {
		var affectedBeatmapIds = ChangeTracker.Entries<BeatmapDto>()
			.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
			.Select(e => e.Entity.Id)
			.ToArray();

		var affectedSetIds = ChangeTracker.Entries<BeatmapsetDto>()
			.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
			.Select(e => e.Entity.Id)
			.ToArray();
		
		var result = await base.SaveChangesAsync(cancellationToken);
		
		if (affectedBeatmapIds.Length > 0 || affectedSetIds.Length > 0)
			await _projectionSync.RefreshAsync(this, affectedBeatmapIds, affectedSetIds, cancellationToken);

		return result;
	}
}

public class BanchoDbContextFactory : IDesignTimeDbContextFactory<BanchoDbContext>
{
	public BanchoDbContext CreateDbContext(
		string[] args
	) {
		var optionsBuilder = new DbContextOptionsBuilder<BanchoDbContext>();
		const string postgresConnectionString =
			$"Host=127.0.0.1;" +
			$"Port=5432;" +
			$"Username=banchonet;" +
			$"Password=banchonet;" +
			$"Database=utopia;" +
			$"Include Error Detail=True;";
		
		optionsBuilder.UseNpgsql(postgresConnectionString);
		
		return new BanchoDbContext(optionsBuilder.Options);
	}
}