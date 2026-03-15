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
	public DbSet<BeatmapDto> Beatmaps { get; init; } = null!;
	public DbSet<BeatmapsetDto> Beatmapsets { get; init; } = null!;
	public DbSet<RelationshipDto> Relationships { get; init; } = null!;
	public DbSet<ScoreDto> Scores { get; init; } = null!;
	public DbSet<LoginDto> PlayerLogins { get; init; } = null!;
	public DbSet<ClientHashesDto> ClientHashes { get; init; } = null!;
	public DbSet<MessageDto> Messages { get; init; } = null!;
	public DbSet<ChannelDto> Channels { get; init; } = null!;

	public DbSet<RefreshToken> RefreshTokens { get; init; } = null!;
	public DbSet<SessionVerification> SessionVerifications { get; init; } = null!;

	protected override void OnModelCreating(
		ModelBuilder modelBuilder
	) {
		modelBuilder
			.ApplyConfiguration(new PlayerConfiguration())
			.ApplyConfiguration(new RelationshipConfiguration())
			.ApplyConfiguration(new BeatmapConfiguration())
			.ApplyConfiguration(new BeatmapsetConfiguration());
		
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
			$"Database=utopia;";
		
		optionsBuilder.UseNpgsql(mySqlConnectionString);
		
		return new BanchoDbContext(optionsBuilder.Options);
	}
}