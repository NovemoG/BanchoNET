using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Db.Configurations;
using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BanchoNET.Core.Models.Db;

public sealed class BanchoDbContext(DbContextOptions<BanchoDbContext> options) : DbContext(options)
{
	public DbSet<PlayerDto> Players { get; init; } = null!;
	public DbSet<StatsDto> Stats { get; init; } = null!;
	public DbSet<RelationshipDto> Relationships { get; init; } = null!;
	public DbSet<LoginDto> PlayerLogins { get; init; } = null!;
	public DbSet<ClientHashesDto> ClientHashes { get; init; } = null!;
	
	public DbSet<ScoreDto> Scores { get; init; } = null!;
	public DbSet<ReplayWatches> ReplayWatches { get; init; } = null!;
	
	public DbSet<BeatmapDto> Beatmaps { get; init; } = null!;
	public DbSet<BeatmapsetDto> Beatmapsets { get; init; } = null!;
	public DbSet<BeatmapOwner> BeatmapOwners { get; init; } = null!;
	public DbSet<BeatmapsetFavorite> BeatmapsetFavorites { get; init; } = null!;
	public DbSet<BeatmapPlays> BeatmapPlays { get; init; } = null!;
	
	public DbSet<MessageDto> Messages { get; init; } = null!;
	public DbSet<ChannelDto> Channels { get; init; } = null!;
	public DbSet<ChannelPlayer> ChannelPlayers { get; init; } = null!;
	
	public DbSet<ThreadDto> Threads { get; init; } = null!;
	public DbSet<CommentDto> Comments { get; init; } = null!;
	public DbSet<ThreadFollows> ThreadFollows { get; init; } = null!;
	public DbSet<CommentVote> CommentVotes { get; init; } = null!;

	public DbSet<ReleaseDto> Releases { get; init; } = null!;
	public DbSet<RefreshToken> RefreshTokens { get; init; } = null!;
	public DbSet<SessionVerification> SessionVerifications { get; init; } = null!;

	protected override void OnModelCreating(
		ModelBuilder modelBuilder
	) {
		modelBuilder
			.ApplyConfiguration(new PlayerConfiguration())
			.ApplyConfiguration(new RelationshipConfiguration())
			.ApplyConfiguration(new BeatmapConfiguration())
			.ApplyConfiguration(new BeatmapsetConfiguration())
			.ApplyConfiguration(new MessageConfiguration())
			.ApplyConfiguration(new ScoreConfiguration())
			.ApplyConfiguration(new SkillsConfiguration())
			.ApplyConfiguration(new CommentConfiguration());
		
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
		
		modelBuilder.Entity<BeatmapOwner>(entity =>
		{
			entity.HasKey(x => new { x.PlayerId, x.BeatmapId });

			entity.HasOne(x => x.Player)
				.WithMany(p => p.OwnedBeatmaps)
				.HasForeignKey(x => x.PlayerId)
				.OnDelete(DeleteBehavior.Cascade);
			
			entity.HasOne(x => x.Beatmap)
				.WithMany(c => c.Owners)
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
		
		base.OnModelCreating(modelBuilder);
	}
}

public class BanchoDbContextFactory : IDesignTimeDbContextFactory<BanchoDbContext>
{
	public BanchoDbContext CreateDbContext(
		string[] args
	) {
		var optionsBuilder = new DbContextOptionsBuilder<BanchoDbContext>();
		const string mySqlConnectionString =
			$"Host=127.0.0.1;" +
			$"Port=5432;" +
			$"Username=banchonet;" +
			$"Password=banchonet;" +
			$"Database=utopia;" +
			$"Include Error Detail=True;";
		
		optionsBuilder.UseNpgsql(mySqlConnectionString);
		
		return new BanchoDbContext(optionsBuilder.Options);
	}
}