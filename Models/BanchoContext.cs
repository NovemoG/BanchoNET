using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models;

public class BanchoContext : DbContext
{
	public BanchoContext(DbContextOptions<BanchoContext> options) : base(options)
	{
		
	}

	public DbSet<Player> Players { get; set; } = null!;
}