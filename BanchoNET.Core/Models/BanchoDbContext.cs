using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BanchoNET.Core.Models;

public sealed class BanchoDbContext(DbContextOptions<BanchoDbContext> options) : DbContext(options)
{
	public DbSet<PlayerDto> Players { get; init; } = null!;
	public DbSet<StatsDto> Stats { get; init; } = null!;
	public DbSet<BeatmapDto> Beatmaps { get; init; } = null!;
	public DbSet<RelationshipDto> Relationships { get; init; } = null!;
	public DbSet<ScoreDto> Scores { get; init; } = null!;
	public DbSet<LoginDto> PlayerLogins { get; init; } = null!;
	public DbSet<ClientHashesDto> ClientHashes { get; init; } = null!;
	public DbSet<MessageDto> Messages { get; init; } = null!;
	public DbSet<ChannelDto> Channels { get; init; } = null!;

	public DbSet<RefreshToken> RefreshTokens { get; init; } = null!;
	public DbSet<SessionVerification> SessionVerifications { get; init; } = null!;
}

public class BanchoDbContextFactory : IDesignTimeDbContextFactory<BanchoDbContext>
{
	public BanchoDbContext CreateDbContext(
		string[] args
	) {
		var optionsBuilder = new DbContextOptionsBuilder<BanchoDbContext>();
		const string mySqlConnectionString =
			$"server=127.0.0.1;" +
			$"port=3306;" +
			$"user=banchonet;" +
			$"password=banchonet;" +
			$"database=utopia;";
		
		optionsBuilder.UseMySQL(mySqlConnectionString);
		
		return new BanchoDbContext(optionsBuilder.Options);
	}
}