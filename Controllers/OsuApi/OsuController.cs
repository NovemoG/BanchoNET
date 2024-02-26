using BanchoNET.Models;
using BanchoNET.Objects;
using BanchoNET.Services;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BanchoNET.Controllers.OsuApi;

[ApiController]
public partial class OsuController : ControllerBase
{
	private readonly BanchoHandler _bancho;
	private readonly GeolocService _geoloc;
	private readonly BanchoSession _session;
	private readonly ServerConfig _config;

	public OsuController(BanchoHandler bancho, GeolocService geoloc, IOptions<ServerConfig> config)
	{
		_bancho = bancho;
		_geoloc = geoloc;
		_session = BanchoSession.Instance;
		_config = config.Value;
	}
	
	[HttpPost("/web/osu-error.php")]
	public async Task<IActionResult> OsuError()
	{
		Console.WriteLine("OsuError");
		return Ok("");
	}

	[HttpGet("/web/bancho_connect.php")]
	public async Task<IActionResult> BanchoConnect()
	{
		Console.WriteLine("BanchoConnect");
		return Ok();
	}

	[HttpGet("/web/check-updates.php")]
	public async Task<IActionResult> CheckUpdates()
	{
		Console.WriteLine("CheckUpdates");
		return Ok();
	}

	[HttpGet("/web/lastfm.php")]
	public async Task<IActionResult> LastFM(
		[FromQuery(Name = "b")]string beatmapId, 
		[FromQuery(Name = "c")]string beatmapNameBase64,
		[FromQuery(Name = "action")]string action,
		[FromQuery(Name = "us")]string username,
		[FromQuery(Name = "ha")]string passwordHash)
	{
		if (beatmapId[0] != 'a') return Ok("-3");

		var flags = (LastFmfLags)int.Parse(beatmapId[1..]);

		if ((flags & (LastFmfLags.HqAssembly | LastFmfLags.HqFile)) != 0)
		{
			//TODO restrict player
			
			//TODO logout player

			return Ok("-3");
		}

		if ((flags & LastFmfLags.RegistryEdits) != 0)
		{
			//TODO
		}
		
		return Ok();
	}

	[HttpPost("/users")]
	public async Task<IActionResult> RegisterAccount(
		[FromForm(Name = "user[username]")] string username, 
		[FromForm(Name = "user[user_email]")] string email, 
		[FromForm(Name = "user[password]")] string password, 
		[FromForm(Name = "Check")] int check)
	{
		if (string.IsNullOrEmpty(username) || 
		    string.IsNullOrEmpty(email) ||
		    string.IsNullOrEmpty(password))
		{
			return BadRequest("Missing required params");
		}

		var errors = new List<ErrorDetails>();

		if (!Regexes.Username.Match(username).Success) errors.Add(new ErrorDetails
		{
			Field = "username",
			Messages = ["Must be 2-15 characters long."]
		});
		if (username.Contains(' ') && username.Contains('_')) errors.Add(new ErrorDetails
		{
			Field = "username",
			Messages = ["Cannot contain spaces and underscores."]
		});
		if (await _bancho.UsernameTaken(username)) errors.Add(new ErrorDetails
		{
			Field = "username",
			Messages = ["Username already taken by other player."]
		});
		
		if (!Regexes.Email.Match(email).Success) errors.Add(new ErrorDetails
		{
			Field = "user_email",
			Messages = ["Invalid email syntax."]
		});
		if (await _bancho.EmailTaken(email)) errors.Add(new ErrorDetails
		{
			Field = "user_email",
			Messages = ["Email already taken by someone else."]
		});

		if (password.Length is <= 8 or > 32) errors.Add(new ErrorDetails
		{
			Field = "password",
			Messages = ["Password must be 8-32 characters long."]
		});

		Console.WriteLine($"Check: {check}");
		if (errors.Count != 0)
		{
			return BadRequest(new
			{
				form_error = new
				{
					user = errors.GroupBy(e => e.Field)
					             .ToDictionary(group => group.Key, group => group
						         .Select(e => string.Join('\n', e.Messages)))
				}
			});
		}
		
		if (check != 0) return Ok("ok"); //if there are no errors but it is a check request
		
		var pwdMD5 = password.CreateMD5();
		var pwdBcrypt = BCrypt.Net.BCrypt.HashPassword(pwdMD5);
		
		_session.InsertPasswordHash(pwdMD5, pwdBcrypt);
		
		var geoloc = await _geoloc.GetGeoloc(Request.Headers);
		await _bancho.CreatePlayer(username, email, pwdBcrypt, geoloc == null ? "xx" : geoloc.Value.Country.Acronym);

		return Ok("ok");
	}

	private readonly record struct ErrorDetails(string Field, List<string> Messages);
}