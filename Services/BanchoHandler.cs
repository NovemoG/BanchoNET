using BanchoNET.Models;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private readonly BanchoSession _session = BanchoSession.Instance;
	private readonly BanchoDbContext _dbContext;
	private readonly HttpClient _httpClient;
	
	private readonly string[] _ignoredChannels = ["#highlight", "#userlog"];

	public BanchoHandler(BanchoDbContext dbContext, HttpClient httpClient)
	{
		_dbContext = dbContext;
		_httpClient = httpClient;
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