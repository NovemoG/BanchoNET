using BanchoNET.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class MultiplayerRepository(BanchoDbContext dbContext)
{
	public async Task<int> GetMatchId()
	{
		var match = await dbContext.MultiplayerMatches.OrderByDescending(m => m.Id).FirstOrDefaultAsync();
		return match?.Id + 1 ?? 1;
	}
}