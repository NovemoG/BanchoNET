using BanchoNET.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models;

public class BanchoDbContext : DbContext
{
	public BanchoDbContext(DbContextOptions<BanchoDbContext> options) : base(options) { }
	
	public DbSet<PlayerDto> Players { get; set; } = null!;
	public DbSet<StatsDto> Stats { get; set; } = null!;
	public DbSet<BeatmapDto> Beatmaps { get; set; } = null!;
	public DbSet<RelationshipDto> Relationships { get; set; } = null!;
	//public DbSet<ScoreDto> Scores { get; set; } = null!;
}