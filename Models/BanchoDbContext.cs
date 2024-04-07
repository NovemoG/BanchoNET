using BanchoNET.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models;

public class BanchoDbContext(DbContextOptions<BanchoDbContext> options) : DbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<PlayerDto>()
		            .Property(p => p.Id)
		            .ValueGeneratedOnAdd()
		            .UseIdentityColumn(3); //TODO this doesn't work, find fix
	}
	
	public DbSet<PlayerDto> Players { get; init; } = null!;
	public DbSet<StatsDto> Stats { get; init; } = null!;
	public DbSet<BeatmapDto> Beatmaps { get; init; } = null!;
	public DbSet<RelationshipDto> Relationships { get; init; } = null!;
	public DbSet<ScoreDto> Scores { get; init; } = null!;
	public DbSet<LoginDto> PlayerLogins { get; init; } = null!;
	public DbSet<ClientHashesDto> ClientHashes { get; init; } = null!;
}