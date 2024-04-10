using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	public async Task<int> GetMatchId()
	{
		var match = await _dbContext.MultiplayerMatches.OrderByDescending(m => m.Id).FirstOrDefaultAsync();
		return match?.Id + 1 ?? 1;
	}
}