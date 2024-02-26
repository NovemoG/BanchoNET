using BanchoNET.Models;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private readonly BanchoSession _session = BanchoSession.Instance;
	private readonly BanchoDbContext _dbContext;
	private readonly ServerConfig _config;
	
	private readonly string[] _ignoredChannels = ["#highlight", "#userlog"];

	public BanchoHandler(BanchoDbContext dbContext, IOptions<ServerConfig> config)
	{
		_dbContext = dbContext;
		_config = config.Value;
	}
	
	public async Task<bool> EmailTaken(string email)
	{
		return await _dbContext.Players.AnyAsync(p => p.Email == email);
	}
	
	public async Task<bool> UsernameTaken(string username)
	{
		return await _dbContext.Players.AnyAsync(p => p.SafeName == username.MakeSafe());
	}
}