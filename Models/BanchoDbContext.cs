using BanchoNET.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models;

public class BanchoDbContext(DbContextOptions<BanchoDbContext> options) : DbContext(options)
{
	public DbSet<PlayerDto> Players { get; set; } = null!;
	public DbSet<StatsDto> Stats { get; set; } = null!;
	public DbSet<BeatmapDto> Beatmaps { get; set; } = null!;
	public DbSet<RelationshipDto> Relationships { get; set; } = null!;
	public DbSet<ScoreDto> Scores { get; set; } = null!;
	public DbSet<LoginDto> PlayerLogins { get; set; } = null!;
	public DbSet<ClientHashesDto> ClientHashes { get; set; } = null!;
}