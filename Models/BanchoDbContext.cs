using BanchoNET.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models;

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
}